import { handleThunks } from 'Store/thunks';
import createHandleActions from './Creators/createHandleActions';
import general from './Settings/general';
import languages from './Settings/languages';

export * from './Settings/general';
export * from './Settings/languages';

//
// Variables

export const section = 'settings';

//
// State

export const defaultState = {
  advancedSettings: false,
  general: general.defaultState,
  languages: languages.defaultState
};

export const persistState = [];

//
// Action Handlers

export const actionHandlers = handleThunks({
  ...general.actionHandlers,
  ...languages.actionHandlers
});

//
// Reducers

export const reducers = createHandleActions({
  ...general.reducers,
  ...languages.reducers

}, defaultState, section);
