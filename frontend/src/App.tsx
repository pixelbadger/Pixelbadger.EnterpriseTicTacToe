import { Navigate, Route, Routes } from "react-router-dom";
import { LobbyPage } from "./pages/LobbyPage";
import { GamePage } from "./pages/GamePage";

export function App() {
  return (
    <Routes>
      <Route path="/" element={<LobbyPage />} />
      <Route path="/join/:code" element={<LobbyPage />} />
      <Route path="/game/:code" element={<GamePage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
