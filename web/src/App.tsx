import { Routes, Route, Navigate } from 'react-router-dom';
import LandingPage from './features/landing/LandingPage';
import StationsPage from './features/stations/StationsPage';

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/stations" element={<StationsPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
