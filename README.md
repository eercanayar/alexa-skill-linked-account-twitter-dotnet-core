# alexa_linkedAccountTwitter.NET
*based on **alexa-csharp-lambda-sample** repo*

linked account implemantation for alexa skills kit using C# netcore10, deploys to amazom lambda
a basic currecy converter application asks EUR/USD, then converts it to TRY. if user wants, it tweets the result to twitter.


**features:**
- uses a middleware to obtain access token from 3rd party such as twitter
- uses session flow to maintain dialogue with user
- connects an API as async to get an info to calculate