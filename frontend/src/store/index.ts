import { configureStore } from "@reduxjs/toolkit";
import { gameApi } from "../store/services/gameApi";

export const store = configureStore({
  reducer: {
    [gameApi.reducerPath]: gameApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(gameApi.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
