export { ApiClient } from './generated-client';
export const apiClient = new ApiClient();
export type {
  Card,
  CardCreateDto,
  CardUpdateDto,
  Deck,
  DeckCreateDto,
  DeckUpdateDto,
  Hand,
  HandCreateDto,
  HandEval,
  HandEvalCreateDto,
  ImportResult,
  TimeSpan
} from './generated-client';
