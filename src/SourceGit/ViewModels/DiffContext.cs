﻿using System.IO;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class DiffContext : ObservableObject
    {
        public string RepositoryPath
        {
            get => _repo;
        }

        public Models.Change WorkingCopyChange
        {
            get => _option.WorkingCopyChange;
        }

        public bool IsUnstaged
        {
            get => _option.IsUnstaged;
        }

        public string FilePath
        {
            get => _option.Path;
        }

        public bool IsOrgFilePathVisible
        {
            get => !string.IsNullOrWhiteSpace(_option.OrgPath) && _option.OrgPath != "/dev/null";
        }

        public string OrgFilePath
        {
            get => _option.OrgPath;
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public bool IsNoChange
        {
            get => _isNoChange;
            private set => SetProperty(ref _isNoChange, value);
        }

        public bool IsTextDiff
        {
            get => _isTextDiff;
            private set => SetProperty(ref _isTextDiff, value);
        }

        public object Content
        {
            get => _content;
            private set => SetProperty(ref _content, value);
        }

        public Vector SyncScrollOffset
        {
            get => _syncScrollOffset;
            set => SetProperty(ref _syncScrollOffset, value);
        }

        public DiffContext(string repo, Models.DiffOption option, DiffContext previous = null)
        {
            _repo = repo;
            _option = option;

            if (previous != null)
            {
                _isNoChange = previous._isNoChange;
                _isTextDiff = previous._isTextDiff;
                _content = previous._content;
            }

            OnPropertyChanged(nameof(FilePath));
            OnPropertyChanged(nameof(IsOrgFilePathVisible));
            OnPropertyChanged(nameof(OrgFilePath));

            Task.Run(() =>
            {
                var latest = new Commands.Diff(repo, option).Result();
                var binaryDiff = null as Models.BinaryDiff;

                if (latest.IsBinary)
                {
                    binaryDiff = new Models.BinaryDiff();

                    var oldPath = string.IsNullOrEmpty(_option.OrgPath) ? _option.Path : _option.OrgPath;
                    if (option.Revisions.Count == 2)
                    {
                        binaryDiff.OldSize = new Commands.QueryFileSize(repo, oldPath, option.Revisions[0]).Result();
                        binaryDiff.NewSize = new Commands.QueryFileSize(repo, _option.Path, option.Revisions[1]).Result();
                    }
                    else
                    {
                        binaryDiff.OldSize = new Commands.QueryFileSize(repo, oldPath, "HEAD").Result();
                        binaryDiff.NewSize = new FileInfo(Path.Combine(repo, _option.Path)).Length;
                    }
                }

                Dispatcher.UIThread.Post(() =>
                {
                    if (latest.IsBinary)
                    {
                        Content = binaryDiff;
                        IsTextDiff = false;
                        IsNoChange = false;
                    }
                    else if (latest.IsLFS)
                    {
                        Content = latest.LFSDiff;
                        IsTextDiff = false;
                        IsNoChange = false;
                    }
                    else if (latest.TextDiff != null)
                    {
                        latest.TextDiff.File = _option.Path;
                        Content = latest.TextDiff;
                        IsTextDiff = true;
                        IsNoChange = false;
                    }
                    else
                    {
                        Content = new Models.NoOrEOLChange();
                        IsTextDiff = false;
                        IsNoChange = true;
                    }

                    IsLoading = false;
                });
            });
        }

        public async void OpenExternalMergeTool()
        {
            var type = Preference.Instance.ExternalMergeToolType;
            var exec = Preference.Instance.ExternalMergeToolPath;

            var tool = Models.ExternalMergeTools.Supported.Find(x => x.Type == type);
            if (tool == null || !File.Exists(exec))
            {
                App.RaiseException(_repo, "Invalid merge tool in preference setting!");
                return;
            }

            var args = tool.Type != 0 ? tool.DiffCmd : Preference.Instance.ExternalMergeToolDiffCmd;
            await Task.Run(() => Commands.MergeTool.OpenForDiff(_repo, exec, args, _option));
        }

        private readonly string _repo = string.Empty;
        private readonly Models.DiffOption _option = null;
        private bool _isLoading = true;
        private bool _isNoChange = false;
        private bool _isTextDiff = false;
        private object _content = null;
        private Vector _syncScrollOffset = Vector.Zero;
    }
}