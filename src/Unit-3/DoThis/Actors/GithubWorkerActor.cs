using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Octokit;

namespace GithubActors.Actors
{
    /// <summary>
    /// Individual actor responsible for querying the Github API
    /// </summary>
    public class GithubWorkerActor : ReceiveActor
    {
        #region Message classes

        public class QueryStarrers
        {
            public QueryStarrers(RepoKey key)
            {
                Key = key;
            }

            public RepoKey Key { get; private set; }
        }

        /// <summary>
        /// Query an individual starrer
        /// </summary>
        public class QueryStarrer
        {
            public QueryStarrer(string login)
            {
                Login = login;
            }

            public string Login { get; private set; }
        }

        public class StarredReposForUser
        {
            public StarredReposForUser(string login, IEnumerable<Repository> repos)
            {
                Repos = repos;
                Login = login;
            }

            public string Login { get; private set; }

            public IEnumerable<Repository> Repos { get; private set; }
        }

        #endregion

        private IGitHubClient _gitHubClient;
        private readonly Func<IGitHubClient> _gitHubClientFactory;

        public GithubWorkerActor(Func<IGitHubClient> gitHubClientFactory)
        {
            _gitHubClientFactory = gitHubClientFactory;
            InitialReceives();
        }

        protected override void PreStart()
        {
            _gitHubClient = _gitHubClientFactory();
        }

        private void InitialReceives()
        {
            // query an individual starrer
            Receive<RetryableQuery>(query => query.Query is QueryStarrer, query =>
            {
                // Resharper disable once PossibleNullReferenceException
                var starrer = (query.Query as QueryStarrer).Login;

                // grab ref to current sender as it could change while async code runs
                var sender = Sender;
                _gitHubClient.Activity.Starring.GetAllForUser(starrer)
                    .ContinueWith<object>(tr =>
                    {
                        // query faulted
                        if (tr.IsFaulted || tr.IsCanceled)
                            return query.NextTry();
                        //query succeeded
                        return new StarredReposForUser(starrer, tr.Result);
                    }).PipeTo(sender);
            });
            
            // query all starrers for a repo
            Receive<RetryableQuery>(query => query.Query is QueryStarrers, query =>
            {
                // Resharper disable once PossibleNullReferenceException
                var starrers = (query.Query as QueryStarrers).Key;

                // grab ref to current sender as it could change while async code runs
                var sender = Sender;
                _gitHubClient.Activity.Starring.GetAllStargazers(starrers.Owner, starrers.Repo)
                    .ContinueWith<object>(tr =>
                    {
                        // query faulted
                        if (tr.IsFaulted || tr.IsCanceled)
                            return query.NextTry();
                        return tr.Result.ToArray();
                    }).PipeTo(sender);
            });
        }
    }
}
