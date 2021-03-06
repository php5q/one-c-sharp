﻿using System;
using System.Linq;
using System.Windows.Input;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Prism.Mvvm;
using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using System.Transactions;
using Zhichkin.Metadata.Notifications;
using Zhichkin.Metadata.Views;
using Zhichkin.Metadata.Model;
using Zhichkin.Metadata.Services;
using Zhichkin.Shell;
using System.Data.SqlClient;
using Microsoft.Practices.Prism.Regions;
using Zhichkin.Metadata.Commands;
using Zhichkin.Metadata.Module;
using System.Windows.Forms;

namespace Zhichkin.Metadata.ViewModels
{
    public class MainMenuViewModel : BindableBase
    {
        private const string CONST_ModuleDialogsTitle = "Z-Metadata";
        private readonly IUnityContainer container;
        private readonly IRegionManager regionManager;
        private readonly IEventAggregator eventAggregator;

        public MainMenuViewModel(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            if (container == null) throw new ArgumentNullException("container");
            if (regionManager == null) throw new ArgumentNullException("regionManager");
            if (eventAggregator == null) throw new ArgumentNullException("eventAggregator");
            this.container = container;
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            this.InfoBaseViewPopup = new InteractionRequest<Confirmation>();
            this.SQLConnectionPopupRequest = new InteractionRequest<SQLConnectionDialogNotification>();

            OpenMetadataCommand = new OpenMetadataCommand<object>(this.OnOpenMetadata, this.CanExecuteCommand);
            SaveMetadataCommand = new DelegateCommand(this.OnSaveMetadata);
            KillMetadataCommand = new DelegateCommand(this.OnKillMetadata);
            UpdateMetadataCommand = new UpdateMetadataCommand<object>(this.OnUpdateMetadata, this.CanExecuteCommand);
            ShowSettingsCommand = new ShowSettingsCommand<object>(this.OnShowSettings, this.CanExecuteCommand);
            AddMetadataCommand = new DelegateCommand(this.OnAddMetadata);
            CreateInfoBaseCommand = new DelegateCommand(this.CreateNewInfoBase);
        }

        private MetadataTreeViewModel MetadataTreeViewModel
        {
            get
            {
                IRegion leftRegion = this.regionManager.Regions[RegionNames.LeftRegion];
                if (leftRegion == null) return null;
                MetadataTreeView view = leftRegion.Views.FirstOrDefault() as MetadataTreeView;
                if (view == null) return null;
                return view.DataContext as MetadataTreeViewModel; ;
            }
        }

        public ICommand OpenMetadataCommand { get; private set; }
        public ICommand SaveMetadataCommand { get; private set; }
        public ICommand KillMetadataCommand { get; private set; }
        public ICommand UpdateMetadataCommand { get; private set; }
        public ICommand ShowSettingsCommand { get; private set; }
        public ICommand AddMetadataCommand { get; private set; }
        public ICommand CreateInfoBaseCommand { get; private set; }

        public InteractionRequest<Confirmation> InfoBaseViewPopup { get; private set; }
        public InteractionRequest<SQLConnectionDialogNotification> SQLConnectionPopupRequest { get; private set; }

        private bool CanExecuteCommand(object args) { return true; }
        private void ClearRightRegion()
        {
            IRegion rightRegion = this.regionManager.Regions[RegionNames.RightRegion];
            if (rightRegion == null) return;
            foreach (object view in rightRegion.Views)
            {
                rightRegion.Remove(view);
            }
        }
        private string GetErrorText(Exception ex)
        {
            string errorText = string.Empty;
            Exception error = ex;
            while (error != null)
            {
                errorText += (errorText == string.Empty) ? error.Message : Environment.NewLine + error.Message;
                error = error.InnerException;
            }
            return errorText;
        }

        private const string CONST_InfoBaseNotSelectedWarning = "Не выбрана информационная база!\nЭто можно сделать при помощи двойного\nщелчка мыши на её наименовании.";

        private void OnSaveMetadata()
        {
            if (this.MetadataTreeViewModel.CurrentInfoBase == null)
            {
                Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = CONST_InfoBaseNotSelectedWarning });
                return;
            }
            try
            {
                this.eventAggregator.GetEvent<MetadataInfoBaseSaveClicked>().Publish(MetadataTreeViewModel.CurrentInfoBase);
                Z.Notify(new Notification
                    {
                        Title = CONST_ModuleDialogsTitle,
                        Content = string.Format(
                            "Метаданные информационной базы \"{0}\" сохранены.",
                            this.MetadataTreeViewModel.CurrentInfoBase.ToString())
                    });
            }
            catch (Exception ex)
            {
                Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = GetErrorText(ex) });
            }
        }
        private void OnKillMetadata()
        {
            if (this.MetadataTreeViewModel.CurrentInfoBase == null)
            {
                Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = CONST_InfoBaseNotSelectedWarning });
                return;
            }
            Z.Confirm(new Confirmation
                {
                    Title = CONST_ModuleDialogsTitle,
                    Content = string.Format(
                        "Информация об информационной базе \"{0}\"\nбудет полностью удалена. Продолжить?",
                        this.MetadataTreeViewModel.CurrentInfoBase.ToString())
                },
                c => { if (c.Confirmed) this.KillMetadata(MetadataTreeViewModel.CurrentInfoBase); });
        }
        private void KillMetadata(InfoBase infoBase)
        {
            try
            {
                TransactionOptions options = new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted };
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.RequiresNew, options))
                {
                    this.eventAggregator.GetEvent<MetadataInfoBaseKillClicked>().Publish(infoBase);
                    scope.Complete();
                }
                MetadataTreeViewModel.InfoBases.Remove(infoBase);
                MetadataTreeViewModel.SetCurrentInfoBase(null);
                ClearRightRegion();
            }
            catch (Exception ex)
            {
                Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = GetErrorText(ex) });
            }
        }
        private void OnUpdateMetadata(object args)
        {
            if (this.MetadataTreeViewModel.CurrentInfoBase == null)
            {
                Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = CONST_InfoBaseNotSelectedWarning });
                return;
            }
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Файлы XML(*.xml)|*.xml" + "|Все файлы (*.*)|*.* ";
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            if (dialog.ShowDialog() != DialogResult.OK) return;
            if (MetadataTreeViewModel == null) return;
            try
            {
                InfoBase sourceInfoBase = new InfoBase();
                (new XMLMetadataAdapter()).Load(dialog.FileName, sourceInfoBase);
                bool cancel = OpenSQLConnectionPopup(sourceInfoBase);
                if (cancel)
                {
                    Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = "Действие отменено пользователем." });
                }
                else
                {
                    InfoBase targetInfoBase = this.MetadataTreeViewModel.CurrentInfoBase;
                    IDifferenceObject difference = (new DifferenceService()).Compare(targetInfoBase, sourceInfoBase);
                    ShowDifferenceObject(difference);
                }
            }
            catch (Exception ex)
            {
                Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = GetErrorText(ex) });
            }
        }
        private void ShowDifferenceObject(IDifferenceObject difference)
        {
            Z.ClearRightRegion(regionManager);
            IRegion rightRegion = this.regionManager.Regions[RegionNames.RightRegion];
            if (rightRegion == null) return;
            object view = this.container.Resolve(
                typeof(DifferenceTreeView),
                new ParameterOverride("model", difference)
                    .OnType(typeof(DifferenceTreeViewModel)));
            if (view == null) return;
            rightRegion.Add(view);
        }
        private void OnShowSettings(object args)
        {
            try
            {
                ShowSettings();
            }
            catch (Exception ex)
            {
                Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = GetErrorText(ex) });
            }
        }
        private void ShowSettings()
        {
            ClearRightRegion();
            IRegion rightRegion = this.regionManager.Regions[RegionNames.RightRegion];
            if (rightRegion == null) return;
            object view = this.container.Resolve(typeof(MetadataSettingsView));
            if (view == null) return;
            rightRegion.Add(view);
        }
        private void OnOpenMetadata(object args)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Файлы XML(*.xml)|*.xml" + "|Все файлы (*.*)|*.* ";
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            if (dialog.ShowDialog() != DialogResult.OK) return;
            if (MetadataTreeViewModel == null) return;
            try
            {
                InfoBase infoBase = new InfoBase();
                (new XMLMetadataAdapter()).Load(dialog.FileName, infoBase);

                bool cancel = true;

                Confirmation confirmation = new Confirmation
                {
                    Title = CONST_ModuleDialogsTitle,
                    Content = "Загрузить метаданные SQL Server ?"
                };
                Z.Confirm(confirmation, c => { cancel = !c.Confirmed; });

                if (cancel)
                {
                    MetadataTreeViewModel.InfoBases.Add(infoBase);
                    return;
                }

                cancel = OpenSQLConnectionPopup(infoBase);
                if (cancel)
                {
                    Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = "Действие отменено пользователем." });
                }
                else
                {
                    MetadataTreeViewModel.InfoBases.Add(infoBase);
                }
            }
            catch (Exception ex)
            {
                Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = GetErrorText(ex) });
            }
        }
        private bool OpenSQLConnectionPopup(InfoBase infoBase)
        {
            bool cancel = false;
            SQLConnectionDialogNotification notification = new SQLConnectionDialogNotification()
            {
                Title = CONST_ModuleDialogsTitle,
                Server = infoBase.Server,
                Database = infoBase.Database,
                UserName = "",
                Password = ""
            };
            this.SQLConnectionPopupRequest.Raise(notification, response =>
            {
                if (!response.Confirmed)
                {
                    cancel = true;
                }
                else
                {
                    this.ImportSQLMetadata(infoBase, response);
                }
            });
            return cancel;
        }
        private void ImportSQLMetadata(InfoBase infoBase, SQLConnectionDialogNotification response)
        {
            SqlConnectionStringBuilder helper = new SqlConnectionStringBuilder()
            {
                DataSource = response.Server,
                InitialCatalog = response.Database,
                IntegratedSecurity = string.IsNullOrWhiteSpace(response.UserName)
            };
            if (!helper.IntegratedSecurity)
            {
                helper.UserID = response.UserName;
                helper.Password = response.Password;
                helper.PersistSecurityInfo = false;
            }
            infoBase.Server = helper.DataSource;
            infoBase.Database = helper.InitialCatalog;
            infoBase.UserName = helper.UserID;
            infoBase.Password = helper.Password;
            (new SQLMetadataAdapter()).Load(helper.ToString(), infoBase);
        }

        private void OnAddMetadata()
        {
            try
            {
                AddMetadata();
            }
            catch (Exception ex)
            {
                Z.Notify(new Notification { Title = CONST_ModuleDialogsTitle, Content = GetErrorText(ex) });
            }
        }
        private void AddMetadata()
        {
            SQLConnectionDialogNotification appInfo = GetConnectionInfo();
            if (appInfo == null) return;
            SQLConnectionDialogNotification sqlInfo = GetConnectionInfo();
            if (sqlInfo == null) return;
            string temp = GetCatalogPath();
            if (string.IsNullOrEmpty(temp)) return;

            XMLMetadataAdapter adapter = new XMLMetadataAdapter();
            string progID = "V83.COMConnector";
            InfoBase infoBase = adapter.GetMetadata(progID, appInfo, temp);
            ImportSQLMetadata(infoBase, sqlInfo);

            MetadataTreeViewModel.InfoBases.Add(infoBase);

            MetadataService service = new MetadataService();
            service.Save(infoBase);
            infoBase.OnPropertyChanged("State");
        }
        private SQLConnectionDialogNotification GetConnectionInfo()
        {
            SQLConnectionDialogNotification notification = new SQLConnectionDialogNotification()
            {
                Title = CONST_ModuleDialogsTitle,
                Server = "",
                Database = "",
                UserName = "",
                Password = ""
            };
            this.SQLConnectionPopupRequest.Raise(notification, response =>
            {
                if (response.Confirmed) notification = response;
                else notification = null;
            });
            return notification;
        }
        private string GetFilePath()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Файлы XML(*.xml)|*.xml" + "|Все файлы (*.*)|*.* ";
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            if (dialog.ShowDialog() != DialogResult.OK) return string.Empty;
            return dialog.FileName;
        }
        private string GetCatalogPath()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return string.Empty;
            return dialog.SelectedPath;
        }

        private void CreateNewInfoBase()
        {
            Confirmation confirmation = new Confirmation()
            {
                Title = CONST_ModuleDialogsTitle,
                Content = new InfoBase()
            };
            this.InfoBaseViewPopup.Raise(confirmation, response =>
            {
                if (response.Confirmed)
                {
                    InfoBase infoBase = response.Content as InfoBase;
                    if (infoBase != null)
                    {
                        MetadataTreeViewModel.InfoBases.Add(infoBase);
                    }
                }
            });
        }
    }
}
