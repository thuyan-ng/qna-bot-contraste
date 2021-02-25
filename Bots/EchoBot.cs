// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.11.1

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

using System.Linq;
using Microsoft.Bot.Builder.AI.QnA;
using AdaptiveCards;
using System.IO;
using System;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;

namespace EchoBotQnAMaker.Bots
{
    public class EchoBot : ActivityHandler
    {
        public QnAMaker EchoBotQnA { get; private set; }
        public EchoBot(QnAMakerEndpoint endpoint)
        {
            // Connects to QnA Maker endpoint for each turn
            EchoBotQnA = new QnAMaker(endpoint);
        }
        private static readonly HttpClient client = new HttpClient();
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Get the question from user
            var question = turnContext.Activity.Text;
            
            // Create an http POST request
            HttpWebRequest httpRequest = ConfigureRequest(question);

            // Get the answer of the request
            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                var obj = JObject.Parse(result);

                // Extract the answer from the QnA knowledge base
                var answer = (string)obj.SelectToken("answers")[0]["answer"];

                // Checks if the answer is a JSON string
                if (answer[0] == '{')
                {
                    var response = turnContext.Activity;
                    response.Attachments = new List<Attachment>() { CreateAdaptiveCardUsingJson(answer) };

                    await turnContext.SendActivityAsync(response, cancellationToken);
                }
                else
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
                }
            }
        }

        private static HttpWebRequest ConfigureRequest(string question)
        {
            var url = "https://qnamic.azurewebsites.net/qnamaker/knowledgebases/55907d96-f2ec-4849-8552-fb8fb68375e8/generateAnswer";
            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Method = "POST";
            httpRequest.Headers["Authorization"] = "EndpointKey 497e3308-0252-4abd-a122-e0dcb3d83e6a";
            httpRequest.ContentType = "application/json";

            //var data = "{\"question\":\"quest\"}";
            var data = Newtonsoft.Json.JsonConvert.SerializeObject(new { question = question });

            using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
            {
                streamWriter.Write(data);
            }
            return httpRequest;
        }

        private Attachment CreateAdaptiveCardUsingJson(string answer)
        {
            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = AdaptiveCard.FromJson(answer).Card
            };
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
