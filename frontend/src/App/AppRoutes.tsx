import React from 'react';
import { Redirect, Route } from 'react-router-dom';
import NotFound from 'Components/NotFound';
import Switch from 'Components/Router/Switch';
import CreatorAdd from 'Creator/CreatorAdd';
import CreatorDetail from 'Creator/CreatorDetail';
import CreatorIndex from 'Creator/CreatorIndex';
import GeneralSettings from 'Settings/General/GeneralSettings';
import MediaManagement from 'Settings/MediaManagement/MediaManagement';
import Profiles from 'Settings/Profiles/Profiles';
import Quality from 'Settings/Quality/Quality';
import Settings from 'Settings/Settings';
import TagSettings from 'Settings/Tags/TagSettings';
import SourcesPage from 'Settings/Sources/SourcesPage';
import UISettings from 'Settings/UI/UISettings';
import Queue from 'Activity/Queue/Queue';
import Backups from 'System/Backup/Backups';
import LogsTable from 'System/Events/LogsTable';
import Logs from 'System/Logs/Logs';
import Status from 'System/Status/Status';
import Tasks from 'System/Tasks/Tasks';
import Updates from 'System/Updates/Updates';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';

function RedirectWithUrlBase() {
  return <Redirect to={getPathWithUrlBase('/')} />;
}

function AppRoutes() {
  return (
    <Switch>
      {/*
        Home
      */}

      <Route exact={true} path="/" component={CreatorIndex} />

      {window.Streamarr.urlBase && (
        <Route
          exact={true}
          path="/"
          // eslint-disable-next-line @typescript-eslint/ban-ts-comment
          // @ts-ignore
          addUrlBase={false}
          render={RedirectWithUrlBase}
        />
      )}

      <Route exact={true} path="/creator/add" component={CreatorAdd} />

      <Route exact={true} path="/creator/:id(\d+)" component={CreatorDetail} />

      <Route path="/creator" component={CreatorIndex} />

      {/*
        Settings
      */}

      <Route exact={true} path="/settings" component={Settings} />

      <Route path="/settings/mediamanagement" component={MediaManagement} />

      <Route path="/settings/profiles" component={Profiles} />

      <Route path="/settings/quality" component={Quality} />

      <Route path="/settings/tags" component={TagSettings} />

      <Route path="/settings/general" component={GeneralSettings} />

      <Route path="/settings/sources" component={SourcesPage} />

      <Route path="/settings/ui" component={UISettings} />

      {/*
        Activity
      */}

      <Route path="/activity/queue" component={Queue} />

      {/*
        System
      */}

      <Route path="/system/status" component={Status} />

      <Route path="/system/tasks" component={Tasks} />

      <Route path="/system/backup" component={Backups} />

      <Route path="/system/updates" component={Updates} />

      <Route path="/system/events" component={LogsTable} />

      <Route path="/system/logs/files" component={Logs} />

      {/*
        Not Found
      */}

      <Route path="*" component={NotFound} />
    </Switch>
  );
}

export default AppRoutes;
