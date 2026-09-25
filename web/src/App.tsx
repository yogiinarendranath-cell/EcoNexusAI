import { Routes, Route, Navigate } from 'react-router-dom';
import LandingPage from './features/landing/LandingPage';
import StationsPage from './features/stations/StationsPage';
import VehiclesPage from './features/vehicles/VehiclesPage';
import JobsPage from './features/jobs/JobsPage';
import JobDetailPage from './features/jobs/JobDetailPage';
import LoginPage from './features/auth/LoginPage';
import RequireCitizen from './features/citizen/RequireCitizen';
import CitizenLayout from './features/citizen/CitizenLayout';
import CitizenHome from './features/citizen/CitizenHome';
import CitizenStations from './features/citizen/CitizenStations';
import CitizenReport from './features/citizen/CitizenReport';
import CitizenClassify from './features/citizen/CitizenClassify';
import CitizenPoints from './features/citizen/CitizenPoints';
import CitizenProfile from './features/citizen/CitizenProfile';
import RecyclingPage from './features/recycling/RecyclingPage';
import FacilityDetailPage from './features/recycling/FacilityDetailPage';

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/stations" element={<StationsPage />} />
      <Route path="/vehicles" element={<VehiclesPage />} />
      <Route path="/jobs" element={<JobsPage />} />
      <Route path="/jobs/:id" element={<JobDetailPage />} />
      <Route path="/recycling" element={<RecyclingPage />} />
      <Route path="/recycling/:id" element={<FacilityDetailPage />} />
      <Route
        path="/app"
        element={
          <RequireCitizen>
            <CitizenLayout />
          </RequireCitizen>
        }
      >
        <Route index element={<CitizenHome />} />
        <Route path="stations" element={<CitizenStations />} />
        <Route path="report" element={<CitizenReport />} />
        <Route path="classify" element={<CitizenClassify />} />
        <Route path="points" element={<CitizenPoints />} />
        <Route path="profile" element={<CitizenProfile />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
