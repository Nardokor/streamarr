import AppSectionState, {
  AppSectionItemState,
  AppSectionSaveState,
} from 'App/State/AppSectionState';
import Language from 'Language/Language';
import General from 'typings/Settings/General';

export interface GeneralAppState
  extends AppSectionItemState<General>,
    AppSectionSaveState {}

export type LanguageSettingsAppState = AppSectionState<Language>;

interface SettingsAppState {
  general: GeneralAppState;
  languages: LanguageSettingsAppState;
}

export default SettingsAppState;
