function Test-UwpNativeAppInstallPackage {

    # Arrange
    $project = New-UwpNativeApp App1

    $project | Install-Package PackageWithNativeCustomControl -Version '1.0.14' -Source $context.RepositoryRoot
    Assert-Package $project PackageWithNativeCustomControl '1.0.14'
}

# install and uninstall package test for .net core
function Test-UwpNativeAppUninstallPackage {

    # Arrange
    $project = New-UwpNativeApp App1
    $project | Install-Package PackageWithNativeCustomControl -Version '1.0.14' -Source $context.RepositoryRoot

    # Act
    $project | Uninstall-Package App1

    Assert-Null (Get-ProjectPackage $project PackageWithNativeCustomControl)
}