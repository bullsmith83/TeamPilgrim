﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using GalaSoft.MvvmLight.Messaging;
using JustAProgrammer.TeamPilgrim.VisualStudio.Business.Services.VisualStudio;
using JustAProgrammer.TeamPilgrim.VisualStudio.Common;
using JustAProgrammer.TeamPilgrim.VisualStudio.Messages;
using JustAProgrammer.TeamPilgrim.VisualStudio.Model;
using JustAProgrammer.TeamPilgrim.VisualStudio.Model.Core;
using JustAProgrammer.TeamPilgrim.VisualStudio.Providers;
using JustAProgrammer.TeamPilgrim.VisualStudio.Windows.Explorer;
using JustAProgrammer.TeamPilgrim.VisualStudio.Windows.PendingChanges;
using JustAProgrammer.TeamPilgrim.VisualStudio.Windows.PendingChanges.Dialogs;
using JustAProgrammer.TeamPilgrim.VisualStudio.Windows.Settings;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using NLog;

namespace JustAProgrammer.TeamPilgrim.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]

    [ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideToolWindow(typeof(ExplorerWindow))]
    [ProvideToolWindow(typeof(PendingChangesWindow))]

    [ProvideOptionPage(typeof(SettingsPage), "Team Pilgrim", "General", 0, 0, true)]

    //http://stackoverflow.com/questions/4478853/vsx-2010-package-loading-markup-xaml-parsing-cannot-find-assemblies
    [ProvideBindingPath]

    [Guid(GuidList.guidTeamPilgrimPkgString)]
    public sealed class TeamPilgrimPackage : Package, IVsShellPropertyEvents
    {
        private static TeamPilgrimPackage _teamPilgrimPackageInstance;

        private static DTE Dte { get; set; }
        private static DTE2 Dte2 { get; set; }

        //public static ExtensionExceptionHandler ExceptionHandler { get; set; }

        private static IVsExtensibility Extensibility { get; set; }

        private static IVsSolution VsSolution;

        //public static ExtensionHost Host { get; set; }

        private static MenuCommandService MenuCommandService { get; set; }

        private static IVsUIShell UIShell { get; set; }

        private static TeamPilgrimVsService _teamPilgrimVsService;

        public static TeamPilgrimServiceModel TeamPilgrimServiceModel { get; private set; }

        public static TeamPilgrimSettings TeamPilgrimSettings { get; private set; }

        private uint _shellPropertyChangeCookie;

        public TeamPilgrimPackage()
        {
            this.Logger().Debug("Start Constructor");

            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));

            TeamPilgrimSettings = new TeamPilgrimSettings();

            //http://blogs.msdn.com/b/vsxteam/archive/2008/06/09/dr-ex-why-does-getservice-typeof-envdte-dte-return-null.aspx
            var shellService = GetGlobalService(typeof(SVsShell)) as IVsShell;
            if (shellService != null)
                ErrorHandler.ThrowOnFailure(shellService.AdviseShellPropertyChanges(this, out _shellPropertyChangeCookie));

            //NOTE: Only enable this when you are looking to debug a particular issue
            //Certain Visual Studio dialogs like the "Work Item Query" can be expected to throw binding errors
            //BindingErrorTraceListener.SetTrace();

            this.Logger().Debug("End Constructor");
        }

        private void ShowExplorerWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(ExplorerWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void ShowPendingChangesWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(PendingChangesWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void ShowUnshelveDetailsDialog(ShowUnshelveDetailsDialogMessage showUnshelveDetailsDialogMessage)
        {
            var unshelveDetailsDialog = new UnshelveDetailsDialog
            {
                DataContext = showUnshelveDetailsDialogMessage.UnshelveDetailsServiceModel
            };

            var dialogResult = unshelveDetailsDialog.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {

            }
        }

        #region Package Members

        protected override void Initialize()
        {
            try
            {

                this.Logger().Info("Initialization: " + VersionInfo.Full);
                this.Logger().Debug("Start First Pass Initialization");

                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
                _teamPilgrimPackageInstance = this;
                base.Initialize();

                // Add our command handlers for menu (commands must exist in the .vsct file)
                OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (null != mcs)
                {
                    // Create the command for the menu item.
                    //                CommandID menuCommandID = new CommandID(GuidList.guidTeamPilgrimCmdSet, (int)PkgCmdIDList.cmdTeamPilgrimExplorer);
                    //                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID );
                    //                mcs.AddCommand( menuItem );

                    // Create the command for the tool window
                    var toolTeamPilgrimExplorer = new MenuCommand(ShowExplorerWindow, new CommandID(GuidList.guidTeamPilgrimCmdSet, (int)PkgCmdIDList.toolTeamPilgrimExplorer));
                    mcs.AddCommand(toolTeamPilgrimExplorer);

                    var cmdTeamPilgrimExplorer = new MenuCommand(ShowExplorerWindow, new CommandID(GuidList.guidTeamPilgrimCmdSet, (int)PkgCmdIDList.cmdTeamPilgrimExplorer));
                    mcs.AddCommand(cmdTeamPilgrimExplorer);

                    var toolTeamPilgrimPendingChanges = new MenuCommand(ShowPendingChangesWindow, new CommandID(GuidList.guidTeamPilgrimCmdSet, (int)PkgCmdIDList.toolTeamPilgrimPendingChanges));
                    mcs.AddCommand(toolTeamPilgrimPendingChanges);

                    var cmdTeamPilgrimPendingChanges = new MenuCommand(ShowPendingChangesWindow, new CommandID(GuidList.guidTeamPilgrimCmdSet, (int)PkgCmdIDList.cmdTeamPilgrimPendingChanges));
                    mcs.AddCommand(cmdTeamPilgrimPendingChanges);
                }

                UIShell = (IVsUIShell)base.GetService(typeof(SVsUIShell));

                if (UIShell == null)
                    throw new ArgumentException("UIShell cannot be null");

                MenuCommandService = base.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

                this.Logger().Debug("End First Pass Initialization");
            }
            catch (Exception exception)
            {
                this.Logger().FatalException("First Pass Initialization", exception);
                throw;
            }

            CompletePackageInitialization();
        }

        public int OnShellPropertyChange(int propid, object var)
        {
            // when zombie state changes to false, finish package initialization

            this.Logger().Trace("OnShellPropertyChange Property:{0}, Value:{1}", propid, var);

            if ((int)__VSSPROPID.VSSPROPID_Zombie == propid)
            {
                if ((bool)var == false)
                {
                    this.Logger().Debug("Shell Exit Zombie State");

                    // zombie state dependent code
                    CompletePackageInitialization();

                    // eventlistener no longer needed

                    var shellService = GetGlobalService(typeof(SVsShell)) as IVsShell;
                    if (shellService != null)
                        ErrorHandler.ThrowOnFailure(shellService.UnadviseShellPropertyChanges(_shellPropertyChangeCookie));

                    _shellPropertyChangeCookie = 0;
                }
            }

            return VSConstants.S_OK;
        }

        private void CompletePackageInitialization()
        {
            try
            {

                this.Logger().Debug("Start Second Pass Initialization");

                if (Dte != null)
                {
                    this.Logger().Warn("Package Previously Initialized");
                    return;
                }

                Dte = GetService(typeof(SDTE)) as DTE;

                if (Dte == null)
                {
                    this.Logger().Warn("DTE not found");
                    return;
                }

                Extensibility = (IVsExtensibility)GetGlobalService(typeof(IVsExtensibility));
                this.Logger().Trace("IVsExtensibility: {0}", Extensibility != null);

                if (Extensibility == null)
                    throw new ArgumentException("Extensibility cannot be null");

                VsSolution = (IVsSolution)GetService(typeof(SVsSolution));
                this.Logger().Trace("IVsSolution: {0}", VsSolution != null);

                Dte2 = (DTE2)Extensibility.GetGlobalsObject(null).DTE;
                this.Logger().Trace("Dte2: {0}", Dte2 != null);

                if (Dte2 == null)
                    throw new ArgumentException("Dte2 cannot be null");

                if (VsSolution == null)
                    throw new ArgumentException("VsSolution cannot be null");

                _teamPilgrimVsService = new TeamPilgrimVsService(_teamPilgrimPackageInstance, UIShell, Dte2, VsSolution);
                TeamPilgrimServiceModel = new TeamPilgrimServiceModel(new TeamPilgrimServiceModelProvider(), _teamPilgrimVsService);

                Messenger.Default.Register<ShowUnshelveDetailsDialogMessage>(this, ShowUnshelveDetailsDialog);

                this.Logger().Debug("End Second Pass Initialization");
            }
            catch (Exception exception)
            {
                this.Logger().FatalException("Second Pass Initialization", exception);
                throw;
            }
        }

        #endregion

        public T GetPackageService<T>()
        {
            if (_teamPilgrimPackageInstance == null)
            {
                return default(T);
            }

            return (T)_teamPilgrimPackageInstance.GetService(typeof(T));
        }
    }
}
