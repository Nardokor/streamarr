import ModelBase from 'App/ModelBase';

export type CommandStatus =
  | 'queued'
  | 'started'
  | 'completed'
  | 'failed'
  | 'aborted'
  | 'cancelled'
  | 'orphaned';

export type CommandResult = 'unknown' | 'successful' | 'unsuccessful';
export type CommandPriority = 'low' | 'normal' | 'high';

export interface BaseCommandBody {
  sendUpdatesToClient: boolean;
  updateScheduledTask: boolean;
  completionMessage: string;
  requiresDiskAccess: boolean;
  isExclusive: boolean;
  isLongRunning: boolean;
  name: string;
  lastExecutionTime: string;
  lastStartTime: string;
  trigger: string;
  suppressMessages: boolean;
}

export type CommandBody = BaseCommandBody;

export interface NewCommandBody {
  name: string;
  priority?: CommandPriority;
  [key: string]: string | number | boolean | number[] | object | undefined;
}

export interface CommandBodyMap {
  ApplicationUpdate: BaseCommandBody;
  Backup: BaseCommandBody;
  ClearLog: BaseCommandBody;
  DeleteLogFiles: BaseCommandBody;
  DeleteUpdateLogFiles: BaseCommandBody;
  ResetApiKey: BaseCommandBody;
  ResetQualityDefinitions: BaseCommandBody;
}

export type CommandBodyForName<T extends keyof CommandBodyMap> =
  CommandBodyMap[T];

interface Command extends ModelBase {
  name: string;
  commandName: string;
  message: string;
  body: CommandBody;
  priority: CommandPriority;
  status: CommandStatus;
  result: CommandResult;
  queued: string;
  started: string;
  ended: string;
  duration: string;
  trigger: string;
  stateChangeTime: string;
  sendUpdatesToClient: boolean;
  updateScheduledTask: boolean;
  lastExecutionTime: string;
}

export default Command;
