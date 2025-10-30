// @ts-check
import eslint from '@eslint/js';
import eslintConfigPrettier from 'eslint-config-prettier';
import eslintPluginPrettier from 'eslint-plugin-prettier';
import globals from 'globals';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  // Ignorar saídas de build e o próprio config
  { ignores: ['dist', 'build', 'coverage', 'eslint.config.mjs'] },

  // Regras base de JS
  eslint.configs.recommended,

  // Regras de TS com type-check
  ...tseslint.configs.recommendedTypeChecked,

  // Opções do parser/projeto
  {
    languageOptions: {
      globals: { ...globals.node, ...globals.jest },
      ecmaVersion: 'latest',
      sourceType: 'module',
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
  },

  // Regras do projeto (qualidade; nada de estilo)
  {
    rules: {
      '@typescript-eslint/no-explicit-any': 'off',
      '@typescript-eslint/no-floating-promises': 'warn',
      '@typescript-eslint/no-unsafe-argument': 'warn',

      // Use a versão TS da regra de unused-vars e permita prefixo "_"
      'no-unused-vars': 'off',
      '@typescript-eslint/no-unused-vars': [
        'warn',
        {
          argsIgnorePattern: '^_',
          varsIgnorePattern: '^_',
          caughtErrorsIgnorePattern: '^_',
        },
      ],

      // Exigir modificadores de acesso explícitos (public/private/protected)
      '@typescript-eslint/explicit-member-accessibility': [
        'warn',
        {
          accessibility: 'explicit',
        },
      ],

      // Warning para identação incorreta (2 espaços)
      '@typescript-eslint/indent': ['warn', 2],
    },
  },

  // Prettier como regra do ESLint
  {
    plugins: {
      prettier: eslintPluginPrettier,
    },
    rules: {
      'prettier/prettier': 'warn',
    },
  },

  // Desliga quaisquer regras de estilo que conflitem com o Prettier
  // (mantenha como **último** item)
  eslintConfigPrettier,
);
