import { Locator, Page } from '@playwright/test';

/**
 * Page Object Model for the first-run roster onboarding flow (/welcome) and the
 * Settings page's roster editor, both of which render the shared RosterPicker component.
 */
export class RosterOnboardingPage {
  readonly page: Page;
  readonly teamTiles: Locator;
  readonly rosterCount: Locator;
  readonly saveButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.teamTiles = page.locator('button.team-tile');
    this.rosterCount = page.locator('.roster-count');
    this.saveButton = page.getByRole('button', { name: /Start my season|Save roster/ });
  }

  async selectFirstNineTeams(): Promise<void> {
    const count = await this.teamTiles.count();
    for (let i = 0; i < 9 && i < count; i++) {
      await this.teamTiles.nth(i).click();
    }
  }

  async save(): Promise<void> {
    await this.saveButton.click();
  }
}
