using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tweetinvi.Core.Parameters;
using Tweetinvi;
using Tweetinvi.Core.Interfaces;
using System.Diagnostics;
using System.Configuration;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace SetanSosmed
{
    public class TwitterAccess
    {
        private string _consumerKey = "";
        private string _consumerSecret = "";
        private string _accessToken = "";
        private string _accessTokenSecret = "";
        private int _dataCount = 50;

        private TwitterCredentials _creds;

        public TwitterAccess(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
            _accessToken = accessToken;
            _accessTokenSecret = accessTokenSecret;
            _creds = new TwitterCredentials(_consumerKey, _consumerSecret, _accessToken, _accessTokenSecret);
            if (!int.TryParse(ConfigurationManager.AppSettings["DataCount"], out _dataCount))
                _dataCount = 50;
        }

        public IEnumerable<ITweet> GrabUserTweet()
        {
            IEnumerable<ITweet> result = null;
            Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                result = Timeline.GetHomeTimeline(5);
            });
            return result;
        }

        public IEnumerable<ITweet> DownloadTweet()
        {
            IEnumerable<ITweet> result = null;
            Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                result = Timeline.GetUserTimeline("mbakyucantik");
            });
            return result;
        }

        public IEnumerable<ITweet> DownloadLikes()
        {
            IEnumerable<ITweet> result = null;
            Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                result = User.GetFavoriteTweets("mbakyucantik");
            });
            return result;
        }

        public bool UnlikeTweet(long id)
        {
            bool isSuccess = false;
            Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                isSuccess = Tweet.UnFavoriteTweet(id);
            });

            return isSuccess;
        }

        public bool DeleteTweet(long id)
        {
            bool isSuccess = false;
            Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                isSuccess = Tweet.DestroyTweet(id);
            });

            return isSuccess;
        }

        public IEnumerable<ITweet> FetchTweetDataFoward(string searchTerm, long sinceId)
        {
            IEnumerable<ITweet> result = null;

            Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                SearchTweetsParameters searchParameter = null;
                if (sinceId > 0)
                {
                    searchParameter = new SearchTweetsParameters(searchTerm)
                    {
                        MaximumNumberOfResults = _dataCount,
                        SearchType = SearchResultType.Recent,
                        Filters = TweetSearchFilters.None,
                        SinceId = sinceId,
                        Lang = LanguageFilter.Indonesian,
                        TweetSearchType = TweetSearchType.OriginalTweetsOnly
                    };
                }
                else
                {
                    searchParameter = new SearchTweetsParameters(searchTerm)
                    {
                        MaximumNumberOfResults = _dataCount,
                        SearchType = SearchResultType.Recent,
                        Filters = TweetSearchFilters.None,
                        SinceId = sinceId,
                        Lang = LanguageFilter.Indonesian,
                        TweetSearchType = TweetSearchType.OriginalTweetsOnly
                    };
                }

                result = Search.SearchTweets(searchParameter);
                if (result != null)
                    Debug.WriteLine(result.Count());
            });

            return result;
        }

        public IEnumerable<ITweet> FetchTweetDataBackward(string searchTerm, long maxID)
        {
            IEnumerable<ITweet> result = null;

            Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                SearchTweetsParameters searchParameter = null;
                if (maxID == 0)
                {
                    searchParameter = new SearchTweetsParameters(searchTerm)
                    {
                        MaximumNumberOfResults = _dataCount,
                        SearchType = SearchResultType.Recent,
                        Filters = TweetSearchFilters.Hashtags
                    };
                }
                else
                {
                    searchParameter = new SearchTweetsParameters(searchTerm)
                    {
                        MaximumNumberOfResults = _dataCount,
                        MaxId = maxID,
                        SearchType = SearchResultType.Recent,
                        Filters = TweetSearchFilters.Hashtags
                    };
                }

                result = Search.SearchTweets(searchParameter);
                if (result != null)
                    Debug.WriteLine(result.Count());
            });

            return result;
        }

        public ITweet PostTweet(string content)
        {
            Auth.SetUserCredentials(_consumerKey, _consumerSecret, _accessToken, _accessTokenSecret);
            ITweet data = Tweet.PublishTweet(content);
            return data;
        }

        public ITweet PostTweetWithImage(string content, byte[] img)
        {
            Auth.SetUserCredentials(_consumerKey, _consumerSecret, _accessToken, _accessTokenSecret);
            var listMedia = new List<byte[]>();
            listMedia.Add(img);
            var Indo = new Coordinates(112.71917, -7.27683);
            ITweet data = Tweet.PublishTweet(content, new PublishTweetOptionalParameters
            {
                MediaBinaries = listMedia,
                PlaceId = "ChIJB0vJuDb0aS4R9oJ8iznVpm4"
            });
            return data;
        }

        public ITweet PostReplyTweet(long replyTo, string content)
        {
            Auth.SetUserCredentials(_consumerKey, _consumerSecret, _accessToken, _accessTokenSecret);
            var tweetToReplyTo = Tweet.GetTweet(replyTo);
            ITweet data = Tweet.PublishTweet(string.Format("{0} {1}", content, tweetToReplyTo.CreatedBy.Name), new PublishTweetOptionalParameters
            {
                InReplyToTweet = tweetToReplyTo,
                AutoPopulateReplyMetadata = true,
                PlaceId = "ChIJB0vJuDb0aS4R9oJ8iznVpm4"
            });
            return data;
        }

        public ITweet PostReplyTweetWithImage(long replyTo, string content, byte[] img)
        {
            Auth.SetUserCredentials(_consumerKey, _consumerSecret, _accessToken, _accessTokenSecret);
            var listMedia = new List<byte[]>();
            listMedia.Add(img);
            var tweetToReplyTo = Tweet.GetTweet(replyTo);
            ITweet data = Tweet.PublishTweet(string.Format("{0} {1}", content, tweetToReplyTo.CreatedBy.Name), new PublishTweetOptionalParameters
            {
                InReplyToTweet = tweetToReplyTo,
                MediaBinaries = listMedia,
                PlaceId = "ChIJB0vJuDb0aS4R9oJ8iznVpm4"
            });
            return data;
        }

        public ITweet PostRetweet(long id)
        {
            var tweet = Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                var result = Tweet.PublishRetweet(id);
                return result;
            });
            ITweet data = tweet;
            return data;
        }

        public bool PostLike(long id)
        {
            var tweet = Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                var result = Tweet.FavoriteTweet(id);
                return result;
            });

            return tweet;
        }

        public bool FollowUser(long id)
        {
            var tweet = Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                var result = User.FollowUser(id);
                return result;
            });
            return tweet;
        }

        public IEnumerable<ITrendLocation> GetLocation()
        {
            var tweet = Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                return Trends.GetAvailableTrendLocations();
            });
            return tweet;
        }

        public IPlaceTrends GetTrends()
        {
            try
            {
                var Indo = new Coordinates(112.71917, -7.27683);
                var tweet = Auth.ExecuteOperationWithCredentials(_creds, () =>
                {

                    var allLoc = Trends.GetAvailableTrendLocations();
                    foreach (var loc in allLoc)
                    {
                        if (loc.CountryCode == "ID")
                        {
                            return Trends.GetTrendsAt(loc.WoeId);
                        }
                    }
                    return Trends.GetTrendsAt(23424846);
                    //return Trends.GetTrendsAt(56000318);
                });
                return tweet;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<IUser> GetFollowers(string screenName)
        {
            var data = Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                var result = User.GetFollowers(screenName, 250);
                return result.ToList();
            });

            return data;
        }

        public IUser GetUsers(string screenName)
        {
            var data = Auth.ExecuteOperationWithCredentials(_creds, () =>
            {
                var user = User.GetUserFromScreenName(screenName);
                return user;
            });

            return data;
        }
    }
}