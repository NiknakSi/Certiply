using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using Polly;

namespace Certiply
{
    /// <summary>
    /// Represents somewhat abstract classes that could be used for more generic purposes
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Checks for the given DNS TXT record containing an expected value until the retry count is exhausted. The 
        /// nameservers of the domain are used to avoid any caching issues.
        /// </summary>
        /// <param name="record">TXT record to look for</param>
        /// <param name="expectedValue">Expected value within the record</param>
        /// <param name="retries">Number of permitted retries, defaults to 100</param>
        /// <param name="interval">Interval between retries, defaults to 30 seconds</param>
        public static async Task<bool> CheckDnsTxtAsync(string record, string expectedValue, CancellationToken cancellationToken, int retries = 100, int interval = 30)
        {
            bool outcome = false;

            var systemClient = new LookupClient
            {
                UseCache = true
            };

            //lookup the nameserver(s) first so we can query them directly and circumvent any caching
            string domainName = record.TrimStart(new char[] { '*', '.' });
            var result = await systemClient.QueryAsync(domainName, QueryType.NS);

            if (cancellationToken.IsCancellationRequested)
                return false;

            //more than likely we'll need to locate the authoritative servers
            if (!result.Answers.Any() && result.Authorities.Any())
                result = await systemClient.QueryAsync(result.Authorities.First().DomainName, QueryType.NS);

            var nameservers = result.Answers.NsRecords();

            if (nameservers.Any())
            {
                var nameserverAddresses = new Dictionary<string, System.Net.IPAddress>();

                foreach (var ns in nameservers)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return false;

                    result = await systemClient.QueryAsync(ns.NSDName, QueryType.A);
                    if (result.Answers.FirstOrDefault() is ARecord nsRecord)
                        nameserverAddresses.Add(ns.NSDName, nsRecord.Address);
                }

                var nsClient = new LookupClient(nameserverAddresses.Select(KeyValuePair => KeyValuePair.Value).ToArray())
                {
                    UseCache = false
                };

                //check all of the answers for the query since multiple txt values are permitted
                //see https://community.letsencrypt.org/t/wildcard-issuance-two-txt-records-for-the-same-name/54528

                bool expectedValueInResults(IDnsQueryResponse query) => query.Answers.Any(r => r is TxtRecord txtRecord && txtRecord.Text.Any(t => t == expectedValue));

                result = await Policy
                    .HandleResult<IDnsQueryResponse>(q => q.HasError || !expectedValueInResults(q))
                    .WaitAndRetryAsync(
                        retries,
                        retryAttempt => TimeSpan.FromSeconds(interval),
                        (delegateResult, timeSpan, context) =>
                        {
                            Console.WriteLine($"Add a DNS TXT record {record} which includes a value of {expectedValue}");

                            if (delegateResult.Result.HasError)
                                Console.WriteLine($"Retrying due to DNS query error: {delegateResult.Result.ErrorMessage}");
                            else
                                Console.WriteLine($"Retrying due to value not found in TXT record");
                        })
                    .ExecuteAsync(async () =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return null;

                        Console.WriteLine($"Checking for TXT record and value at {record}");
                        return await nsClient.QueryAsync(record, QueryType.TXT);
                    });

                if (result != null && !result.HasError && expectedValueInResults(result))
                    outcome = true;
            }

            return outcome;
        }
    }
}
