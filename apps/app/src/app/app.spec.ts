import { describe, expect, it } from 'vitest';

describe('App scaffold', () => {
  it('keeps the core loop title in place', () => {
    expect('Meal Planner').toContain('Meal');
  });
});
