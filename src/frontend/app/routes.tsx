import Bugsnag from "@bugsnag/js";
import React, { useEffect, useState, lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";

import Avatar from "./components/Avatar";
import NotificationCenter from "./components/NotificationCenter/NotificationCenter";
import Loader from "./components/Loader";
import { useGetCurrentUserQuery } from "./store/serviceApi";

const PUBLIC_ROUTE_PATTERNS = [/^\/privacy$/, /^\/tournaments$/, /^\/tournaments\/[^/]+$/];
const AUTH_FAILURE_DIAGNOSTIC_KEY = "refhub-auth-last-failure";

function getErrorStatus(error: unknown): number | undefined {
  if (!error || typeof error !== "object") {
    return undefined;
  }

  const candidate = error as { status?: unknown };
  return typeof candidate.status === "number" ? candidate.status : undefined;
}

const PrivacyPolicy = lazy(() => import("./pages/PrivacyPolicy"));
const RefereeProfile = lazy(() => import("./pages/RefereeProfile"));
const Admin = lazy(() => import("./pages/Admin"));
const Test = lazy(() => import("./pages/Test"));
const StartTest = lazy(() => import("./pages/StartTest"));
const NgbProfile = lazy(() => import("./pages/NgbProfile"));
const ImportWizard = lazy(() => import("./pages/ImportWizard"));
const RefereeTests = lazy(() => import("./pages/RefereeTests"));
const Tournament = lazy(() => import("./pages/Tournaments"));
const TournamentDetails = lazy(() => import("./pages/Tournaments/TournamentId"));
const TeamView = lazy(() => import("./pages/TeamView"));
const TeamManagement = lazy(() => import("./pages/TeamManagement"));
const bugsnagEnabled = Boolean(process.env.BUGSNAG_API_KEY);

const App = () => {
  const isPublicRoute = PUBLIC_ROUTE_PATTERNS.some((pattern) => pattern.test(window.location.pathname));
  const [redirectTo, setRedirectTo] = useState<string>();
  const { currentData: currentUser, error, isError, isLoading } = useGetCurrentUserQuery();
  const roles = (currentUser?.roles?.map((r) => r.roleType).filter((r): r is string => Boolean(r)) ?? []);

  const ownedNgbIds = currentUser
    ? (() => {
        const ngb = (currentUser.roles?.find((r) => r.roleType === "NgbAdmin") as any)?.ngb;
        if (typeof ngb === "string") {
          return [ngb];
        }

        return ngb;
      })()
    : undefined;
  const shouldShowSignInButton = isPublicRoute && !currentUser;

  const getRedirect = () => {
    if (roles.includes("IqaAdmin")) return "/admin";
    if (roles.includes("NgbAdmin")) return `/national_governing_bodies/${ownedNgbIds[0]}`;
    if (roles.includes("Referee") && currentUser?.userId) return `/referees/${currentUser.userId}`;

    return undefined;
  };

  useEffect(() => {
    const status = getErrorStatus(error);
    const shouldRedirectToSignIn = status === 401 || status === 403;

    if (!isLoading && isError && shouldRedirectToSignIn && !isPublicRoute) {
      try {
        const latestDiagnostic = sessionStorage.getItem(AUTH_FAILURE_DIAGNOSTIC_KEY);
        if (latestDiagnostic) {
          console.warn("[AuthDiagnostic] redirecting to sign-in", JSON.parse(latestDiagnostic));
        } else {
          console.warn("[AuthDiagnostic] redirecting to sign-in without prior diagnostic", {
            status,
            route: window.location.pathname,
            search: window.location.search,
          });
        }
      } catch {
        console.warn("[AuthDiagnostic] redirecting to sign-in (diagnostic unavailable)", {
          status,
          route: window.location.pathname,
          search: window.location.search,
        });
      }

      window.location.href = `${window.location.origin}/sign_in`;
    }
  }, [isError, isLoading, isPublicRoute, error]);

  useEffect(() => {
    if (currentUser) {
      setRedirectTo(getRedirect());
    }
  }, [currentUser, roles]);

  useEffect(() => {
    if (bugsnagEnabled && currentUser?.userId) {
      Bugsnag.setUser(currentUser.userId);
    }
  }, [bugsnagEnabled, currentUser?.userId]);

  if (isLoading === true) return <Loader />;

  return (
    <Suspense fallback={<Loader />}>
      <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <div>
          <div className="bg-navy-blue text-right text-white py-3 px-10 flex items-center justify-end">
            <p className="flex-shrink mx-8">Management Hub</p>
            {currentUser?.userId && <NotificationCenter currentUserId={currentUser.userId} />}
            {shouldShowSignInButton && (
              <a
                href="/sign_in"
                className="inline-flex items-center px-4 py-2 mr-4 text-sm font-semibold rounded bg-white text-navy-blue hover:bg-gray-100"
              >
                Sign In
              </a>
            )}
            {currentUser?.userId && (
              <Avatar
                firstName={currentUser.firstName ?? ""}
                lastName={currentUser.lastName ?? ""}
                roles={roles}
                userId={currentUser.userId}
                ownedNgbId={ownedNgbIds ? ownedNgbIds[0] : undefined}
                enabledFeatures={
                  /* FUTURE: currentUser?.enabledFeatures when feature flags are implemented */
                  []
                }
              />
            )}
          </div>
          <Routes>
            <Route path="/" element={(redirectTo && <Navigate to={redirectTo} replace />) || <></>} />
            <Route path="/privacy" element={<PrivacyPolicy />} />
            <Route path="/referees/:refereeId" element={<RefereeProfile />} />
            <Route path="/admin" element={<Admin />} />
            <Route path="/admin/tests/:testId" element={<Test />} />
            <Route path="/referees/:refereeId/tests/:testId" element={<StartTest />} />
            <Route path="/national_governing_bodies/:ngbId" element={<NgbProfile />} />
            <Route path="/import/:importScope/:scopeId" element={<ImportWizard />} />
            <Route path="/referees/:refereeId/tests" element={<RefereeTests />} />
            <Route path="/tournaments" element={<Tournament />} />
            <Route path="/tournaments/:tournamentId" element={<TournamentDetails />} />
            <Route path="/teams/:teamId" element={<TeamView />} />
            <Route path="/teams/:teamId/manage" element={<TeamManagement />} />
            {/* FUTURE: Settings route when i18n feature is implemented
            {currentUser?.enabledFeatures.includes("i18n") ? (
              <Route
                path="/settings"
                element={<Settings />}
              />
            ) : null} */}
          </Routes>
        </div>
      </BrowserRouter>
    </Suspense>
  );
};

export default App;
