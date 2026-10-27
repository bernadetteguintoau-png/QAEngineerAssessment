# QA Engineer Assessment

Automated UI test for the [Snipe-IT demo](https://demo.snipeitapp.com), written in C# with Playwright and NUnit.

## What the test does

- Login to the snipeit demo at https://demo.snipeitapp.com/login
- Create a new Macbook Pro 13" asset with the ready to deploy status and checked out to a random user
- Find the asset you just created in the assets list to verify it was created successfully
- Navigate to the asset page from the list and validate relevant details from the asset creation
- Validate the details in the "History" tab on the asset page


## Prerequisites
- .NET 10 SDK
- Visual Studio 2026
- PowerShell (for installing the Playwright browser)


## Setup

From the project folder:
    dotnet build
    .\bin\Debug\net10.0\playwright.ps1 install chromium


## Run the test
1.    dotnet test

2. To watch the browser while the test runs:
      $env:HEADED=1; dotnet test

3. You can also run it from Visual Studio via
      Test > Test Explorer.
