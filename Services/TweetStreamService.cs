using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreTweet;
using CoreTweet.Streaming;

namespace ChucoAnalyzeTwitter.Services
{
    public interface ITweetStreamService
    {
        void InitStream();
    }

    public class TweetStreamService: ITweetStreamService
    {
        private readonly List<String> TwitterTokenEnvs = new List<String>()
        {
            "CONSUMER_KEY",
            "CONSUMER_SECRET",
            "OAUTH_ACCESS_TOKEN",
            "OAUTH_ACCESS_SECRET"
        };

        private Tokens MyToken;
        private Subject<object> StreamRestarter;
        private bool isInitialized = false;

        public TweetStreamService() 
        {
            var systemEnv = Environment.GetEnvironmentVariables();
            if (TwitterTokenEnvs.Count(req => systemEnv.Contains(req)) != TwitterTokenEnvs.Count) {
                throw new Exception($"Environmet must contain {TwitterTokenEnvs}");
            }

            var CONSUMER_KEY = Environment.GetEnvironmentVariable(TwitterTokenEnvs[0]);
            var CONSUMER_SECRET = Environment.GetEnvironmentVariable(TwitterTokenEnvs[1]);
            var OAUTH_ACCESS_TOKEN = Environment.GetEnvironmentVariable(TwitterTokenEnvs[2]);
            var OAUTH_ACCESS_SECRET = Environment.GetEnvironmentVariable(TwitterTokenEnvs[3]);

            MyToken = Tokens.Create(CONSUMER_KEY, CONSUMER_SECRET, OAUTH_ACCESS_TOKEN, OAUTH_ACCESS_SECRET);
            StreamRestarter = new Subject<object>();
        }

        public void InitStream()
        {
            if (isInitialized) return;
            isInitialized = false;

            var disposable = ConnectStream();

            StreamRestarter.Subscribe(async _ => {
                disposable.Dispose();
                await Task.Delay(30 * 1000);
                disposable = ConnectStream();
            });
        }

        private IDisposable ConnectStream()
        {
            var stream = MyToken.Streaming.UserAsObservable().Publish();

            stream.OfType<DisconnectMessage>()
            .Subscribe(x => {
                if (x.Code == DisconnectCode.Shutdown) {
                    StreamRestarter.OnNext(null);
                }
            });

            stream.OfType<StatusMessage>()
            .Subscribe(x => System.Console.WriteLine($"{x.Status.User.ScreenName}: {x.Status.Text}"));

            return stream.Connect();
        }
    }
}