﻿using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SourceGit.Commands
{
    public partial class QueryRevisionObjects : Command
    {
        [GeneratedRegex(@"^\d+\s+(\w+)\s+([0-9a-f]+)\s+(.*)$")]
        private static partial Regex REG_FORMAT();

        public QueryRevisionObjects(string repo, string sha, string parentFolder)
        {
            WorkingDirectory = repo;
            Context = repo;
            Args = ["ls-tree", sha];

            if (!string.IsNullOrEmpty(parentFolder))
                Args.AddRange(["--", parentFolder]);
        }

        public List<Models.Object> Result()
        {
            Exec();
            return _objects;
        }

        protected override void OnReadline(string line)
        {
            var match = REG_FORMAT().Match(line);
            if (!match.Success)
                return;

            var obj = new Models.Object();
            obj.SHA = match.Groups[2].Value;
            obj.Type = Models.ObjectType.Blob;
            obj.Path = match.Groups[3].Value;

            switch (match.Groups[1].Value)
            {
                case "blob":
                    obj.Type = Models.ObjectType.Blob;
                    break;
                case "tree":
                    obj.Type = Models.ObjectType.Tree;
                    break;
                case "tag":
                    obj.Type = Models.ObjectType.Tag;
                    break;
                case "commit":
                    obj.Type = Models.ObjectType.Commit;
                    break;
            }

            _objects.Add(obj);
        }

        private List<Models.Object> _objects = new List<Models.Object>();
    }
}
