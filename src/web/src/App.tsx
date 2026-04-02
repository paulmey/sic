import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider, useAuth } from './AuthContext';
import Layout from './pages/Layout';
import LoginPage from './pages/LoginPage';
import ResourcesPage from './pages/ResourcesPage';
import ResourceDetailPage from './pages/ResourceDetailPage';
import AdminPage from './pages/AdminPage';
import ManageCategoriesPage from './pages/ManageCategoriesPage';
import ManageResourcesPage from './pages/ManageResourcesPage';
import ResourceRolesPage from './pages/ResourceRolesPage';
import ManageUsersPage from './pages/ManageUsersPage';
import ManageInvitesPage from './pages/ManageInvitesPage';
import InvitePage from './pages/InvitePage';
import ProfilePage from './pages/ProfilePage';
import './App.css';

function AppRoutes() {
  const { user, loading } = useAuth();

  if (loading) return <div className="loading">Loading...</div>;
  if (!user) return (
    <Routes>
      <Route path="/invite/:inviteId" element={<InvitePage />} />
      <Route path="*" element={<LoginPage />} />
    </Routes>
  );

  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<ResourcesPage />} />
        <Route path="profile" element={<ProfilePage />} />
        <Route path="resources/:resourceId" element={<ResourceDetailPage />} />
        <Route path="admin" element={<AdminPage />} />
        <Route path="admin/categories" element={<ManageCategoriesPage />} />
        <Route path="admin/resources" element={<ManageResourcesPage />} />
        <Route path="admin/resources/:resourceId/roles" element={<ResourceRolesPage />} />
        <Route path="admin/users" element={<ManageUsersPage />} />
        <Route path="admin/invites" element={<ManageInvitesPage />} />
      </Route>
    </Routes>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}
