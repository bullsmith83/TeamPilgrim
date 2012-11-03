﻿using System;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace JustAProgrammer.TeamPilgrim.VisualStudio.Providers
{
    public interface IPilgrimServiceModelProvider
    {
        bool TryGetCollections(out TfsTeamProjectCollection[] collections);
        
        bool TryGetCollection(out TfsTeamProjectCollection collection, Uri tpcAddress);
        
        bool TryGetProjects(out Project[] projects, Uri tpcAddress);

        bool TryDeleteQueryDefinition(out bool result, TfsTeamProjectCollection teamProjectCollection, Project teamProject, Guid queryId);

        bool TryGetBuildDefinitionsByProjectName(out IBuildDefinition[] buildDefinitions, TfsTeamProjectCollection collection, string projectName);

        bool TryCloneQueryDefinition(out IBuildDefinition buildDefinition, TfsTeamProjectCollection collection, Project project, IBuildDefinition definition);
    }
}
