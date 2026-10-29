#:package Microsoft.Identity.Client@4.92.0
#:property TargetFramework=net10.0

// Dataverse Web API(OData v4.0)로 조회하는 샘플
// 실행: dotnet run odata.cs -- [환경 호스트 이름]

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Identity.Client;

// TODO: org00000000.crm5.dynamics.com은 예시 값이므로 실제 환경의 호스트 이름으로 바꾸거나 첫 번째 인수로 지정
// 호스트 이름: Power Platform 관리 센터의 환경 URL 또는 pac org list의 Environment URL에서 https://와 끝의 /를 제외한 부분
var host = args.ElementAtOrDefault(0) ?? "org00000000.crm5.dynamics.com";

if (host.StartsWith("org00000000.", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("호스트 이름이 예시 값(org00000000)입니다. 실제 환경의 호스트 이름을 지정하십시오.");
    Console.Error.WriteLine("예: dotnet run odata.cs -- <환경>.crm5.dynamics.com");
    return 1;
}

var resource = $"https://{host}";

// Microsoft Learn의 Dataverse 샘플 코드가 공용으로 사용하는 앱 등록 (개발 및 테스트 전용)
// 실제 앱에서는 Microsoft Entra ID에 직접 등록한 앱의 클라이언트 ID를 사용
var app = PublicClientApplicationBuilder.Create("51f81489-12ee-4a9e-aaae-a2591f45987d")
    .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
    .WithRedirectUri("http://localhost")
    .Build();

var token = await app.AcquireTokenInteractive([$"{resource}/.default"]).ExecuteAsync();

using var http = new HttpClient
{
    BaseAddress = new Uri($"{resource}/api/data/v9.2/"),
    Timeout = TimeSpan.FromMinutes(2),
};
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
http.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
http.DefaultRequestHeaders.Add("OData-Version", "4.0");
http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

// WhoAmI 함수 호출
using (var whoAmI = JsonDocument.Parse(await http.GetStringAsync("WhoAmI")))
    Console.WriteLine($"사용자 ID: {whoAmI.RootElement.GetProperty("UserId").GetGuid()}");

// tds-sql.cs와 같은 조건의 OData 쿼리
using var response = await http.GetAsync("accounts?$select=name,createdon&$orderby=createdon desc&$top=5");
var body = await response.Content.ReadAsStringAsync();
if (!response.IsSuccessStatusCode)
{
    Console.Error.WriteLine($"{(int)response.StatusCode} {body}");
    return 1;
}

using var document = JsonDocument.Parse(body);
foreach (var row in document.RootElement.GetProperty("value").EnumerateArray())
{
    var name = row.TryGetProperty("name", out var value) && value.ValueKind == JsonValueKind.String
        ? value.GetString()
        : "(이름 없음)";
    Console.WriteLine($"{row.GetProperty("createdon").GetDateTime():yyyy-MM-dd HH:mm}  {name}");
}
return 0;
