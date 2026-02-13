import { createApi, fetchBaseQuery } from "@reduxjs/toolkit/query/react";
import type { GameState, JoinGameRequest, MoveRequest, StartGameRequest } from "../../types/game";

export const gameApi = createApi({
  reducerPath: "gameApi",
  baseQuery: fetchBaseQuery({
    baseUrl: "/api",
    credentials: "include",
  }),
  tagTypes: ["Game"],
  endpoints: (builder) => ({
    startGame: builder.mutation<GameState, StartGameRequest>({
      query: (body) => ({
        url: "/games/start",
        method: "POST",
        body,
      }),
    }),
    joinGame: builder.mutation<GameState, JoinGameRequest>({
      query: (body) => ({
        url: "/games/join",
        method: "POST",
        body,
      }),
      invalidatesTags: (_result, _error, arg) => [{ type: "Game", id: arg.sessionCode }],
    }),
    getGameState: builder.query<GameState, string>({
      query: (sessionCode) => `/games/${sessionCode}`,
      providesTags: (_result, _error, sessionCode) => [{ type: "Game", id: sessionCode }],
    }),
    makeMove: builder.mutation<GameState, { sessionCode: string; body: MoveRequest }>({
      query: ({ sessionCode, body }) => ({
        url: `/games/${sessionCode}/moves`,
        method: "POST",
        body,
      }),
    }),
    requestRematch: builder.mutation<GameState, string>({
      query: (sessionCode) => ({
        url: `/games/${sessionCode}/rematch`,
        method: "POST",
      }),
    }),
  }),
});

export const {
  useStartGameMutation,
  useJoinGameMutation,
  useGetGameStateQuery,
  useMakeMoveMutation,
  useRequestRematchMutation,
} = gameApi;
