﻿using System;

namespace Certiply
{
    //TODO: Write an implementation which uses SQL Server for certificate and order storage
    public class SqlServerCertManager : ICertManager
    {
        public string AccountKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string CN => throw new NotImplementedException();

        public string OrderUri { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CertPrivateKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CertIssuer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Certificate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string InitForCommonName(string cn)
        {
            throw new NotImplementedException();
        }
    }
}
