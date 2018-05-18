using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.DirectoryServices.Protocols;
using System.Security;
using System.Net;

namespace OfficeMapper.Lib
{
    public class LDAPHelper
    {
        private readonly LdapConnection ldapConnection;
        private readonly string searchBaseDN;
        private readonly int pageSize;

        public LDAPHelper(
            string searchBaseDN,
            string hostName,
            int portNumber,
            AuthType authType,
            int pageSize)
        {

            var ldapDirectoryIdentifier = new LdapDirectoryIdentifier(
                hostName,
                portNumber,
                true,
                false);

            //var networkCredential = new NetworkCredential(
            //    connectionAccountName,
            //    connectionAccountPassword);

            ldapConnection = new LdapConnection(
                ldapDirectoryIdentifier)
            { AuthType = authType };

            ldapConnection.SessionOptions.ProtocolVersion = 3;

            this.searchBaseDN = searchBaseDN;
            this.pageSize = pageSize;
        }

        public IEnumerable<SearchResultEntryCollection> PagedSearch(
            string searchFilter,
            string[] attributesToLoad)
        {

            var pagedResults = new List<SearchResultEntryCollection>();

            var searchRequest = new SearchRequest
                    (searchBaseDN,
                     searchFilter,
                     SearchScope.Subtree,
                     attributesToLoad);


            var searchOptions = new SearchOptionsControl(SearchOption.DomainScope);
            searchRequest.Controls.Add(searchOptions);

            var pageResultRequestControl = new PageResultRequestControl(pageSize);
            searchRequest.Controls.Add(pageResultRequestControl);

            while (true)
            {
                var searchResponse = (SearchResponse)ldapConnection.SendRequest(searchRequest);
                var pageResponse = (PageResultResponseControl)searchResponse.Controls[0];

                yield return searchResponse.Entries;
                if (pageResponse.Cookie.Length == 0)
                    break;

                pageResultRequestControl.Cookie = pageResponse.Cookie;
            }


        }
    }
}