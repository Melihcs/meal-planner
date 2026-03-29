import js from '@eslint/js';
import angular from 'angular-eslint';
import eslintConfigPrettier from 'eslint-config-prettier';
import globals from 'globals';
import tseslint from 'typescript-eslint';

const angularTypeScriptFiles = ['apps/app/**/*.ts'];
const angularTemplateFiles = ['apps/app/**/*.html'];

export default tseslint.config(
  {
    ignores: [
      '**/node_modules/**',
      '**/.pnpm-store/**',
      '**/.turbo/**',
      '**/.angular/**',
      '**/coverage/**',
      '**/dist/**',
      '**/www/**',
      '**/platforms/**',
      '**/plugins/**',
      '**/android/**',
      '**/ios/**',
      '**/release/**',
      '**/bin/**',
      '**/obj/**',
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...angular.configs.tsRecommended.map((config) => ({
    ...config,
    files: angularTypeScriptFiles,
  })),
  ...angular.configs.templateRecommended.map((config) => ({
    ...config,
    files: angularTemplateFiles,
  })),
  ...angular.configs.templateAccessibility.map((config) => ({
    ...config,
    files: angularTemplateFiles,
  })),
  {
    files: ['**/*.ts'],
    languageOptions: {
      parser: tseslint.parser,
    },
    rules: {
      '@typescript-eslint/consistent-type-imports': 'error',
    },
  },
  {
    files: angularTypeScriptFiles,
    languageOptions: {
      globals: globals.browser,
    },
    processor: angular.processInlineTemplates,
  },
  {
    files: ['apps/electron/**/*.ts', 'apps/app/capacitor.config.ts'],
    languageOptions: {
      globals: globals.node,
    },
  },
  eslintConfigPrettier,
);
