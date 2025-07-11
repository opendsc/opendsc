@{
    # This is a PowerShell data file that contains test data for the OpenDsc.Resource.Windows.User utility
    testCases = @(
        @{
            operation = 'get'
            testData = @(
                @{
                    username = 'Administrator'    
                }
            )
            requiresElevation = $false
        },
        @{
            operation = 'set'
            testData = @(
                @{
                    username = 'TestUser'
                    password = 'P@ssw0rd!'
                    description = 'Test User Account'
                    passwordNeverExpires = $true
                    accountDisabled = $false
                    passwordChangeRequired = $false
                    passwordChangeNotAllowed = $false
                }
            )
            requiresElevation = $true
        },
        @{
            operation = 'delete'
            testData = @(
                @{
                    username = 'TestUser'
                }
            )
            requiresElevation = $true
        },
        @{
            operation = 'export'
            testData = @{
                username = "does not have to exist"
            }
            requiresElevation = $false
        }
    )  
}