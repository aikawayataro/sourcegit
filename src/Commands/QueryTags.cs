﻿using System;
using System.Collections.Generic;

namespace SourceGit.Commands
{
    public class QueryTags : Command
    {
        public QueryTags(string repo)
        {
            Context = repo;
            WorkingDirectory = repo;
            Args = ["tag", "-l", "--sort=-creatordate", "--format=$%(refname)$%(objectname)$%(*objectname)"];
        }

        public List<Models.Tag> Result()
        {
            Exec();
            return _loaded;
        }

        protected override void OnReadline(string line)
        {
            var subs = line.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (subs.Length == 2)
            {
                _loaded.Add(new Models.Tag()
                {
                    Name = subs[0].Substring(10),
                    SHA = subs[1],
                });
            }
            else if (subs.Length == 3)
            {
                _loaded.Add(new Models.Tag()
                {
                    Name = subs[0].Substring(10),
                    SHA = subs[2],
                });
            }
        }

        private readonly List<Models.Tag> _loaded = new List<Models.Tag>();
    }
}
