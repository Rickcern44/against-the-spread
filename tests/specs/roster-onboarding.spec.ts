import { expect, test } from '@playwright/test';
import { RosterOnboardingPage } from '../pages/roster-onboarding-page';

/**
 * First-run roster onboarding gate. The Phase 4 app UI (Dashboard/Picks/MyStats/Settings) runs
 * entirely against the deterministic FakeApiClient until the real HTTP-backed IAppApiClient is
 * wired in (see Program.cs), so a fresh app load always starts with no roster saved - this spec
 * exercises the real gating/routing and RosterPicker component in the browser, not seeded Azurite
 * state.
 */
test.describe('First-run roster onboarding', () => {
  test('fresh app load is redirected to /welcome before any other page renders', async ({ page }) => {
    await page.goto('/');

    await expect(page).toHaveURL(/\/welcome$/);
    await expect(page.getByRole('heading', { name: 'Draft your roster' })).toBeVisible();
  });

  test('drafting nine teams lands on the dashboard and settings shows the same roster', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/welcome$/);

    const onboarding = new RosterOnboardingPage(page);
    await onboarding.selectFirstNineTeams();
    await expect(onboarding.rosterCount).toContainText('9 / 9 selected');
    await onboarding.save();

    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('heading', { name: 'Your dashboard' })).toBeVisible();

    // Client-side navigation (not page.goto): a full browser navigation would reload the WASM
    // app and, with it, the in-memory FakeApiClient/RosterState this spec is exercising.
    await page.getByRole('link', { name: 'Settings' }).click();
    await expect(page).toHaveURL(/\/settings$/);
    const settings = new RosterOnboardingPage(page);
    await expect(settings.rosterCount).toContainText('9 / 9 selected');
  });
});
