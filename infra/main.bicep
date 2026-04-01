targetScope = 'resourceGroup'

@description('Base name for all resources')
param baseName string = 'sic'

@description('Azure region')
param location string = resourceGroup().location

@description('Cosmos DB free tier (one per subscription)')
param cosmosFreeTier bool = true

// --- Cosmos DB ---
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: '${baseName}-cosmos'
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableFreeTier: cosmosFreeTier
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    capabilities: [
      { name: 'EnableServerless' }
    ]
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosAccount
  name: 'sic'
  properties: {
    resource: {
      id: 'sic'
    }
  }
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'sic-data'
  properties: {
    resource: {
      id: 'sic-data'
      partitionKey: {
        paths: ['/pk']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          { path: '/type/?' }
          { path: '/startTime/?' }
          { path: '/endTime/?' }
          { path: '/resourceId/?' }
          { path: '/categoryId/?' }
          { path: '/identityProvider/?' }
          { path: '/identityId/?' }
          { path: '/expiresAt/?' }
          { path: '/usedByUserId/?' }
        ]
        excludedPaths: [
          { path: '/*' }
          { path: '/"_etag"/?' }
        ]
      }
    }
  }
}

// --- Static Web App ---
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: '${baseName}-swa'
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    buildProperties: {
      appLocation: 'src/web'
      apiLocation: 'src/api/Sic.Api'
      outputLocation: 'dist'
    }
  }
}

// --- Link Cosmos connection string to SWA ---
resource swaAppSettings 'Microsoft.Web/staticSites/config@2023-12-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    CosmosDbConnectionString: cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
  }
}

// --- Outputs ---
output staticWebAppName string = staticWebApp.name
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname
output cosmosAccountName string = cosmosAccount.name
