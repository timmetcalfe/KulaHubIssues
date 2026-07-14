import { test, expect } from '@playwright/test';

test.describe('Feedback page', () => {
  test('displays the feedback form', async ({ page }) => {
    await page.goto('/Feedback');

    await expect(page).toHaveTitle(/Feedback/);
    await expect(page.getByRole('heading', { name: 'Feedback' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Name' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Email address' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Comments' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Submit feedback' })).toBeVisible();
  });

  test('shows Feedback link in navigation', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('link', { name: 'Feedback' })).toBeVisible();
  });

  test('submits the feedback form and shows success message', async ({ page }) => {
    await page.goto('/Feedback');

    await page.getByRole('textbox', { name: 'Name' }).fill('Jane Smith');
    await page.getByRole('textbox', { name: 'Email address' }).fill('jane.smith@example.com');
    await page.getByRole('textbox', { name: 'Comments' }).fill('This is a great CRM system. Very intuitive!');
    await page.getByRole('button', { name: 'Submit feedback' }).click();

    await expect(page.getByRole('alert')).toContainText('Thank you for your feedback!');
    await expect(page.getByRole('textbox', { name: 'Name' })).not.toBeVisible();
  });

  test('shows validation errors when form fields are empty', async ({ page }) => {
    await page.goto('/Feedback');

    await page.getByRole('button', { name: 'Submit feedback' }).click();

    await expect(page.getByRole('textbox', { name: 'Name' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Email address' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Comments' })).toBeVisible();
  });

  test('shows validation error for invalid email', async ({ page }) => {
    await page.goto('/Feedback');

    await page.getByRole('textbox', { name: 'Name' }).fill('John Doe');
    await page.getByRole('textbox', { name: 'Email address' }).fill('not-an-email');
    await page.getByRole('textbox', { name: 'Comments' }).fill('Some comments');
    await page.getByRole('button', { name: 'Submit feedback' }).click();

    await expect(page.getByRole('textbox', { name: 'Email address' })).toBeVisible();
  });
});
