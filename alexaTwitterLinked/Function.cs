using System.Net.Http;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Tweetinvi;
using Tweetinvi.Exceptions;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace alexaTwitterLinked
{
    public class Function
    {

        public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
        {

            var requestType = input.GetRequestType();
            if (requestType == typeof(IntentRequest))
            {
                var intentRequest = input.Request as IntentRequest;
                var intentName = intentRequest?.Intent?.Name;
                if (intentName == "calculateExchange")
                {
                    var amountData = intentRequest?.Intent?.Slots["Amount"].Value;
                    var currencyVal = intentRequest?.Intent?.Slots["Currency"].Value;
                    int amountVal;

                    if (int.TryParse(amountData, out amountVal) && (currencyVal == "dollars" || currencyVal == "euros"))
                    {

                        var exchangeResult = await calculateViaApi(amountVal, currencyVal, context);

                        Dictionary<string, object> sessionAttr = new Dictionary<string, object>();

                        string speechOutput = amountVal.ToString() + " " + currencyVal + " equals to " + exchangeResult + " Turkish Liras.";
                        sessionAttr.Add("tweetCache", speechOutput);

                        return MakeSkillResponse(
                            speechOutput,
                             false,
                             "You can ask another or say \"tweet\" to tweet the result.",
                             sessionAttr);

                    }
                    else
                    {
                        return MakeSkillResponse(
                        $"I didn't understand currency and amount, may you ask again?",
                        false
                        );
                    }

                }
                else if (intentName == "tweetResult")
                {
                    
                    string[] currentAccessToken;
                    if (input.Session.User.AccessToken == null)
                        return MakeSkillResponse($"Please link your account to the skill using Alexa App.", false);
                    else
                        currentAccessToken = input.Session.User.AccessToken.Split(',');

                    object retValue;
                    string tweetContent;

                    if (input.Session.Attributes.TryGetValue("tweetCache", out retValue))
                        tweetContent = retValue.ToString();
                    else
                        return MakeSkillResponse($"Oh! There is nothing to tweet.", false);

                    Auth.SetUserCredentials("YOUR_KEY_HERE", "YOUR_SECRET_HERE", currentAccessToken[0], currentAccessToken[1]);
                    var authenticatedUser = Tweetinvi.User.GetAuthenticatedUser();
                    if (authenticatedUser == null)
                    {
                        return MakeSkillResponse($"Please link your account to the skill using Alexa App.", false);
                    }

                    ExceptionHandler.SwallowWebExceptions = false;

                    try
                    {
                        var firstTweet = Tweet.PublishTweet(tweetContent);
                    }
                    catch (ArgumentException)
                    {
                        return MakeSkillResponse($"Hmmm, there is a problem while tweeting.", false);
                    }
                    catch (TwitterException)
                    {
                        return MakeSkillResponse($"Hmmm, there is a problem while tweeting.", false);
                    }

                    return MakeSkillResponse(
                        $"I have just tweeted that; " + tweetContent,
                        true);

                }
                else if (intentName == "AMAZON.StopIntent")
                {
                    return MakeSkillResponse(
                        $"Goodbye!",
                        true);
                }
                else if (intentName == "AMAZON.CancelIntent")
                {
                    return MakeSkillResponse(
                        $"Goodbye!",
                        true);
                }
                else
                {
                    return MakeSkillResponse(
                        $"I don't understand what to do. You can try agian.",
                        false);
                }
               
            } else if (requestType == typeof(LaunchRequest))
            {
                
                return MakeSkillResponse(
                    $"Welcome to exchange calculator. You can ask any exchange to Turkish liras.",
                    false);
                
            }
            else
            {
                return MakeSkillResponse(
                        $"I don't understand what to do. You can try agian.",
                        false);
            }
        }


        private SkillResponse MakeSkillResponse(string outputSpeech,
            bool shouldEndSession,
            string repromptText = "You can ask an exchange by saying like; \"convert 10 dollars\". To exit, say, exit.",
            Dictionary<string, object> sessionAttr = null)
        {
            var response = new ResponseBody
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = new PlainTextOutputSpeech { Text = outputSpeech }
            };

            if (repromptText != null)
            {
                response.Reprompt = new Reprompt() { OutputSpeech = new PlainTextOutputSpeech() { Text = repromptText } };
            }

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = "1.0",
                SessionAttributes = sessionAttr
            };
            return skillResponse;
        }

        private async Task<int> calculateViaApi(int amountVal, string currencyVal, ILambdaContext context)
        {
            Dictionary<string, double> currencyDict = new Dictionary<string, double>();
            int result = 0;
            JToken ratesData=null;
            
            try
            {
                HttpClient client = new HttpClient();
                Task<string> getStringTask = client.GetStringAsync("http://api.fixer.io/latest?base=TRY");
                string urlContents = await getStringTask;
                ratesData = JToken.Parse(urlContents);
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"\nException: {ex.Message}");
                context.Logger.LogLine($"\nStack Trace: {ex.StackTrace}");
            }

            
            currencyDict.Add("euros", Convert.ToDouble(ratesData?["rates"]?["EUR"]?.ToString()));
            currencyDict.Add("dollars", Convert.ToDouble(ratesData?["rates"]?["USD"]?.ToString()));
            
            //currencyDict.Add("euros", 0.24592);
            //currencyDict.Add("dollars", 0.28072);

            result = (int)Math.Round(amountVal / currencyDict[currencyVal]);
            return result;
        }
    }
}
