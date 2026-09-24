import { Routes, Route, Navigate } from 'react-router-dom';
import LandingPage from './features/landing/LandingPage';
import StationsPage from './features/stations/StationsPage';
import VehiclesPage from './features/vehicles/VehiclesPage';
import JobsPage from './features/jobs/JobsPage';
import JobDetailPage from './features/jobs/JobDetailPage';
import LoginPage from './features/auth/LoginPage';

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/stations" element={<StationsPage />} />
      <Route path="/vehicles" element={<VehiclesPage />} />
      <Route path="/jobs" element={<JobsPage />} />
      <Route path="/jobs/:id" element={<JobDetailPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
