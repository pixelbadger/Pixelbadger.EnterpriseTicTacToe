targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module sql 'sql/sql.bicep' = {
  name: 'sql'
  scope: rg
  params: {
    location: location
  }
}

module app_service_env_acr 'app-service-env-acr/app-service-env-acr.bicep' = {
  name: 'app-service-env-acr'
  scope: rg
  params: {
    location: location
  }
}

module app_service_env 'app-service-env/app-service-env.bicep' = {
  name: 'app-service-env'
  scope: rg
  params: {
    location: location
    app_service_env_acr_outputs_name: app_service_env_acr.outputs.name
    userPrincipalId: principalId
  }
}

module api_identity 'api-identity/api-identity.bicep' = {
  name: 'api-identity'
  scope: rg
  params: {
    location: location
  }
}

module api_roles_sql 'api-roles-sql/api-roles-sql.bicep' = {
  name: 'api-roles-sql'
  scope: rg
  params: {
    location: location
    sql_outputs_name: sql.outputs.name
    sql_outputs_sqlserveradminname: sql.outputs.sqlServerAdminName
    principalId: api_identity.outputs.principalId
    principalName: api_identity.outputs.principalName
  }
}

output app_service_env_acr_name string = app_service_env_acr.outputs.name

output app_service_env_acr_loginServer string = app_service_env_acr.outputs.loginServer

output app_service_env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = app_service_env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID

output app_service_env_AZURE_CONTAINER_REGISTRY_ENDPOINT string = app_service_env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT

output app_service_env_planId string = app_service_env.outputs.planId

output app_service_env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID string = app_service_env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID

output sql_sqlServerFqdn string = sql.outputs.sqlServerFqdn

output api_identity_id string = api_identity.outputs.id

output api_identity_clientId string = api_identity.outputs.clientId

output app_service_env_AZURE_APP_SERVICE_DASHBOARD_URI string = app_service_env.outputs.AZURE_APP_SERVICE_DASHBOARD_URI

output app_service_env_AZURE_WEBSITE_CONTRIBUTOR_MANAGED_IDENTITY_ID string = app_service_env.outputs.AZURE_WEBSITE_CONTRIBUTOR_MANAGED_IDENTITY_ID

output app_service_env_AZURE_WEBSITE_CONTRIBUTOR_MANAGED_IDENTITY_PRINCIPAL_ID string = app_service_env.outputs.AZURE_WEBSITE_CONTRIBUTOR_MANAGED_IDENTITY_PRINCIPAL_ID