# Verbit

A product that allows the creation, taking, and grading of verb tests, intended for use in primary school
language classes.

# Building

- Requires .NET 6 or greater
- Requires NPM for the front-end project, in order to use Tailwind
- Uses Tailwind 3

To run the Tailwind compiler in watch mode for dev purposes, run this from the repo root:

```
npx tailwindcss -i ./Styles/app.css -o .\wwwroot\app.css --watch --verbose
```