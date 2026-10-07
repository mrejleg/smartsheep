#!/usr/bin/env ruby
# Local development only. Never print or commit the generated client secret.
require 'json'
require 'digest'
require 'open3'

mobile_dir = File.expand_path('..', __dir__)
api_dir = File.expand_path('../SmartSheep/src/02.Applications/01.WebApi/04.Api', mobile_dir)
defines_path = File.join(mobile_dir, '.env.local.json')

unless File.exist?(defines_path)
  settings = JSON.parse(File.read(File.join(api_dir, 'appsettings.json')))
  development_path = File.join(api_dir, 'appsettings.Development.json')
  development = File.exist?(development_path) ? JSON.parse(File.read(development_path)) : {}
  key = ENV['Security__EncryptKeyMD5'] || development.dig('Security', 'EncryptKeyMD5') || settings.dig('Security', 'EncryptKeyMD5')
  abort 'The local API encryption key is missing.' if key.nil? || key.empty?

  client_id = 'SmartSheep'
  # Match AuthApiService.GetClientSecret / TextExtentions.GetEncodeTextMd5.
  client_secret = Digest::SHA256.hexdigest(key + Digest::SHA256.hexdigest(client_id) + key)
  defines = JSON.pretty_generate({
    'API_CLIENT_ID' => client_id,
    'API_CLIENT_SECRET' => client_secret
  })
  patch = "*** Begin Patch\n*** Add File: #{defines_path}\n" + defines.lines.map { |line| '+' + line }.join + "\n*** End Patch\n"
  File.umask(0o077)
  _, _, status = Open3.capture3('apply_patch', stdin_data: patch)
  abort 'Could not create the private local Flutter configuration.' unless status.success?
  File.chmod(0o600, defines_path)
end

defines = JSON.parse(File.read(defines_path))
abort 'API_CLIENT_SECRET is missing in .env.local.json.' if defines['API_CLIENT_SECRET'].to_s.empty?
puts 'Starting mobile with private local API configuration.'
Dir.chdir(mobile_dir)
exec 'flutter', 'run', '--dart-define-from-file=.env.local.json', *ARGV
