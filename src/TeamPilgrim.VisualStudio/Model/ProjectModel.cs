﻿using System.Threading;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using JustAProgrammer.TeamPilgrim.VisualStudio.Common;
using JustAProgrammer.TeamPilgrim.VisualStudio.Model.ProjectModels;
using JustAProgrammer.TeamPilgrim.VisualStudio.Model.QueryItemModels;
using JustAProgrammer.TeamPilgrim.VisualStudio.Providers;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace JustAProgrammer.TeamPilgrim.VisualStudio.Model
{
    public class ProjectModel : BaseModel, IQueryItemCommandModel
    {
        private readonly IPilgrimServiceModelProvider _pilgrimServiceModelProvider;

        public TfsTeamProjectCollection ProjectCollection { get; private set; }

        public Project Project { get; private set; }

        public TeamPilgrimModel TeamPilgrimModel { get; private set; }

        public ProjectBuildModel ProjectBuildModel { get; private set; }

        public BaseModel[] ChildObjects { get; private set; }

        public ProjectModel(IPilgrimServiceModelProvider pilgrimServiceModelProvider, TfsTeamProjectCollection projectCollection, Project project, ProjectBuildModel projectBuildModel)
        {
            _pilgrimServiceModelProvider = pilgrimServiceModelProvider;
            ProjectCollection = projectCollection;
            Project = project;
            ProjectBuildModel = projectBuildModel;
            
            OpenSourceControlCommand = new RelayCommand(OpenSourceControl, CanOpenSourceControl);
            OpenQueryDefinitionCommand = new RelayCommand<QueryDefinitionModel>(OpenQueryDefinition, CanOpenQueryDefinition);
            EditQueryDefinitionCommand = new RelayCommand<QueryDefinitionModel>(EditQueryDefinition, CanEditQueryDefinition);
            DeleteQueryDefinitionCommand = new RelayCommand<QueryDefinitionModel>(DeleteQueryDefinition, CanDeleteQueryDefinition);

            ChildObjects = new BaseModel[]
                {
                    new WorkItemsModel(Project.QueryHierarchy, this), 
                    new ReportsModel(),
                    new BuildsModel(projectBuildModel),
                    new TeamMembersModel(),
                    new SourceControlModel()
                };
        }

        protected override void OnActivated()
        {
            VerifyCalledOnUiThread();

            ProjectBuildModel.Activate();

            if (ThreadPool.QueueUserWorkItem(PilgrimProjectCallback))
            {
                State = ModelStateEnum.Fetching;
            }
        }

        #region OpenQueryItem

        public RelayCommand<QueryDefinitionModel> OpenQueryDefinitionCommand { get; private set; }

        private void OpenQueryDefinition(QueryDefinitionModel queryDefinitionModel)
        {
            TeamPilgrimPackage.TeamPilgrimVsService.OpenQueryDefinition(ProjectCollection, queryDefinitionModel.QueryDefinition.Id);
        }

        private bool CanOpenQueryDefinition(QueryDefinitionModel queryDefinitionModel)
        {
            return true;
        }

        #endregion

        #region EditQueryItem

        public RelayCommand<QueryDefinitionModel> EditQueryDefinitionCommand { get; private set; }

        private void EditQueryDefinition(QueryDefinitionModel queryDefinitionModel)
        {
            TeamPilgrimPackage.TeamPilgrimVsService.EditQueryDefinition(ProjectCollection, queryDefinitionModel.QueryDefinition.Id);
        }

        private bool CanEditQueryDefinition(QueryDefinitionModel queryDefinitionModel)
        {
            return true;
        }

        #endregion

        #region DeleteQueryItem

        public RelayCommand<QueryDefinitionModel> DeleteQueryDefinitionCommand { get; private set; }

        private void DeleteQueryDefinition(QueryDefinitionModel queryDefinitionModel)
        {
            bool result;

            var queryId = queryDefinitionModel.QueryDefinition.Id;

            if(_pilgrimServiceModelProvider.TryDeleteQueryDefinition(out result, ProjectCollection, Project, queryId))
            {
                if(result)
                {
                    TeamPilgrimPackage.TeamPilgrimVsService.CloseQueryDefinitionFrames(ProjectCollection, queryId);
                    
                }
            }
        }

        private bool CanDeleteQueryDefinition(QueryDefinitionModel queryDefinitionModel)
        {
            return true;
        }

        #endregion

        #region OpenSourceControl

        public RelayCommand OpenSourceControlCommand { get; private set; }

        private void OpenSourceControl()
        {
            TeamPilgrimPackage.TeamPilgrimVsService.OpenSourceControl(Project.Name);
        }

        private bool CanOpenSourceControl()
        {
            return true;
        }

        #endregion


        private void PilgrimProjectCallback(object state)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new ThreadStart(delegate
                {
                    State = ModelStateEnum.Active;
                }));
        }


    }
}
