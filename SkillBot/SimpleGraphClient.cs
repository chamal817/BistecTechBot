using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Graph;
using Azure.Identity;
using Azure;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.ExternalConnectors;
using BDO.Bot.BDOSkillBot.Objects;
using Newtonsoft.Json.Linq;

namespace BDOSkillBot
{
    // This class is a wrapper for the Microsoft Graph API
    // See: https://developer.microsoft.com/en-us/graph
    public class SimpleGraphClient
    {
        private readonly string _token;
        private readonly IConfiguration _configuration;
        public SimpleGraphClient(string token, IConfiguration configuration)
        {
            _configuration = configuration;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            _token = token;
        }


        private GraphServiceClient GetAuthenticatedClient()
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    requestMessage =>
                    {
                        //Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", _token);

                        //Get event times in the current time zone.

                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Local.Id + "\"");

                        return Task.CompletedTask;
                    }));



            return graphClient;
        }


        
        public async Task<UserInfo> GetUserEmail()
        {
            var logingUser = new UserInfo();

            try
            {
                var graphClient = GetAuthenticatedClient();
                var me = await graphClient.Me.Request().GetAsync();
                logingUser.Email = me.Mail;
                logingUser.Name = me.DisplayName;
     
            }
            catch (Exception ex)
            {

            }
            return logingUser;
        }

    }
}
