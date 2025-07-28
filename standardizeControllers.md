Standardize the controllers (AuthController.cs, OrganizationsController.cs, SubscriptionsController.cs, UsersController.cs) by appling the following changes:

1. Remove Try-Catch Blocks: Rely on GlobalExceptionHandlingMiddleware for unhandled exceptions.
2. Use ValidateRequest: Call ValidateRequest at the start of actions that accept FromBody requests, passing the FluentValidation result if you manually validate using a validator.
3. Use HandleServiceResult: Use HandleServiceResult for all service calls to handle success and error cases consistently.
4. Remove Custom Error Handling: Eliminate specific exception catching (e.g., ex.Message.Contains("already has an active subscription")) and ensure the service layer returns appropriate ServiceResult objects with error messages that trigger the correct HTTP status codes in HandleServiceResult.
5. Align with ValidationErrorResponse: Ensure validation errors return a ValidationErrorResponse consistent with the structure defined in Program.cs.
