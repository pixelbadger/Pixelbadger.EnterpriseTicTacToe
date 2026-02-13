@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource dbup_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('dbup_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = dbup_identity.id

output clientId string = dbup_identity.properties.clientId

output principalId string = dbup_identity.properties.principalId

output principalName string = dbup_identity.name

output name string = dbup_identity.name