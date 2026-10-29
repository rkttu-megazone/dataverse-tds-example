# Dataverse TDS 엔드포인트 SQL 접속 방법

| 항목 | 내용 |
| --- | --- |
| 확인 일자 | 2026년 10월 29일 |
| 대상 환경 | `org00000000.crm5.dynamics.com` (ContactCenter Trial) |
| 인증 | Microsoft Entra ID 사용자 계정 |

> **참고**
>
> 본 문서와 샘플 코드의 `org00000000.crm5.dynamics.com`은 예시 값이므로 실제 환경의 호스트 이름으로 바꾸십시오. 호스트 이름은 Power Platform 관리 센터의 환경 URL이나 `pac org list` 결과의 Environment URL에서 `https://`와 끝의 `/`를 제외한 부분이며, `crm5`처럼 지역에 따라 달라지는 부분도 그대로 사용합니다. 데이터베이스 이름으로 쓰인 `org00000000`은 호스트 이름의 첫 부분으로 바꾸십시오. 정확한 데이터베이스 이름은 5절의 `select db_name()`으로 확인할 수 있습니다.

## 1. 개요

Dataverse는 TDS(Tabular Data Stream) 엔드포인트로 SQL Server와 호환되는 읽기 전용 쿼리를 제공합니다. 본 문서는 테스트에서 확인한 2가지 접속 방법과 데이터베이스, 테이블, 열 정보를 조회하는 방법을 정리합니다.

| 도구 | 인증 방식 |
| --- | --- |
| sqlcmd (ODBC) 15.0 | `-G` (Microsoft Entra ID 대화형 인증) |
| Visual Studio 2026 서버 탐색기 | Microsoft Entra MFA |

## 2. 사전 점검 항목

| 항목 | 확인 방법 |
| --- | --- |
| 네트워크 포트 | `Test-NetConnection <호스트 이름> -Port 1433` 실행 결과의 `TcpTestSucceeded : True` 확인 (5558번 포트도 사용 가능) |
| 환경 접근 권한 | `pac org list`에 대상 환경 표시 여부 확인 |
| TDS 엔드포인트 활성화 | Power Platform 관리 센터 > 환경 > 설정 > 제품 > 기능에서 TDS 엔드포인트 활성화 여부 확인 (기본값 활성화) |
| TDS 엔드포인트 사용 권한 | 사용자 수준 액세스 제어가 켜진 환경이면 보안 역할에 Allow user to access TDS endpoint 권한 필요 |

## 3. sqlcmd (ODBC)

다음 명령을 실행하십시오. `org00000000.crm5.dynamics.com`은 실제 환경의 호스트 이름으로, `user@contoso.com`은 실제 계정으로 바꾸십시오.

```powershell
& "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE" -S org00000000.crm5.dynamics.com -G -U user@contoso.com -l 30 -Q "select top 5 name from account"
```

실행 결과는 다음과 같습니다.

```
name
----
Trey Research
Coffee Lab APJ
Fourth Coffee
Graphic Design Institute

(4 rows affected)
```

> **참고**
>
> - TDS 엔드포인트를 처음 사용하는 시점에는 로그인 후 `Initialization failed/timed out, please connect again` 오류가 발생할 수 있습니다. 이 경우 같은 명령을 다시 실행하십시오. 본 테스트에서는 첫 연결이 성공한 이후 같은 오류가 다시 발생하지 않았습니다.
> - `-S`에는 `https://`와 끝의 `/`를 제외한 호스트 이름만 입력하십시오.
> - `-l`(로그인 제한 시간)은 30~60초를 권장합니다.
> - 다른 버전의 sqlcmd가 PATH에 먼저 등록되어 있을 수 있으므로 ODBC 버전은 위와 같이 전체 경로로 실행하십시오.

## 4. Visual Studio 2026

서버 탐색기에서 SQL Server 연결을 추가하는 기본 경로를 그대로 사용하며, 인증 방법만 Microsoft Entra MFA로 지정합니다.

1. 서버 탐색기를 여십시오.
2. 데이터 연결에서 SQL Server 연결 추가를 선택하십시오.
3. 연결 창에 아래 표의 값을 입력하고 연결하십시오.
4. 로그인 창이 뜨면 Microsoft Entra ID 계정으로 로그인하십시오.

| 항목 | 입력값 |
| --- | --- |
| 서버 이름 | `org00000000.crm5.dynamics.com` |
| 인증 | Microsoft Entra MFA |
| 사용자 이름 | Microsoft Entra ID 계정(UPN) |
| 데이터베이스 이름 | `org00000000` (직접 입력) |

> **참고**
>
> 데이터베이스 이름은 드롭다운 목록을 펼치지 말고 직접 입력하십시오. 목록 조회는 Dataverse에서 지원되지 않습니다. 표의 `org00000000`은 예시 값이므로 실제 환경의 값으로 바꾸십시오. 데이터베이스 이름은 5절의 `select db_name()`으로 확인할 수 있습니다.

## 5. 데이터베이스와 테이블 조회

Dataverse TDS 엔드포인트는 환경 1개를 데이터베이스 1개로 제공합니다. 다른 환경의 데이터는 해당 환경의 호스트 이름으로 별도 접속하여 조회하며, 접근 가능한 환경 목록은 `pac org list`로 확인합니다.

| 용도 | 쿼리 | 대상 환경 조회 결과 |
| --- | --- | --- |
| 현재 데이터베이스 이름 | `select db_name()` | `org00000000` |
| 데이터베이스 목록 | `select name from sys.databases` | `org00000000` 1건 |
| 스키마 목록 | `select name from sys.schemas` | `dbo` 1건 |
| 테이블 목록 | `select name from sys.tables order by name` | 1,495건 |
| 테이블의 열 목록 | 아래 쿼리 참조 | `account` 테이블 240건 |

테이블의 열 이름과 데이터 형식은 다음 쿼리로 조회합니다. `account`를 원하는 테이블 이름으로 바꾸십시오.

```sql
select c.name as column_name, ty.name as type_name
from sys.columns c
join sys.tables t on c.object_id = t.object_id
join sys.types ty on c.user_type_id = ty.user_type_id
where t.name = 'account'
order by c.column_id
```

> **참고**
>
> - 대상 환경의 데이터베이스 이름은 호스트 이름의 첫 부분과 같았습니다. 다른 환경에서는 `select db_name()`으로 확인하십시오.
> - `org00000000.dbo.account`처럼 데이터베이스, 스키마, 테이블을 모두 지정한 이름을 사용할 수 있습니다.
> - 저장 프로시저(`EXEC sp_databases` 등), `USE`, `INFORMATION_SCHEMA.COLUMNS`는 지원되지 않으므로 위의 `sys` 카탈로그 뷰를 사용하십시오.

## 6. 데이터 접근 범위와 보안

### 6.1 읽기 전용

TDS 엔드포인트로 연결하면 데이터 조회만 가능합니다. 공식 문서는 SQL 연결이 읽기 전용 액세스를 제공한다고 명시하며, 데이터 조작(DML), 데이터 정의(DDL), 트랜잭션 제어(TCL), 권한 제어(DCL), 저장 프로시저를 지원하지 않습니다. 대상 행이 0건인 수정 문장도 실행 전 단계에서 거부됩니다.

| 실행 문장 | 결과 |
| --- | --- |
| `update account set name = name where 1 = 0` | `'UPDATE' statement is not supported` |
| `delete from account where 1 = 0` | `'DELETE' statement is not supported` |

읽기 전용은 TDS 연결 경로에만 적용됩니다. 같은 계정이라도 Power Apps, Web API, SDK로 접근하면 보안 역할이 허용하는 범위에서 데이터를 수정할 수 있습니다.

### 6.2 보안 모델

TDS 엔드포인트는 Dataverse 보안 모델을 그대로 적용합니다. 사용자는 보안 역할에서 조회 권한을 받은 테이블과 행만 조회할 수 있습니다.

### 6.3 사용자 수준 액세스 제어

Power Platform 관리 센터 > 환경 > 설정 > 제품 > 기능의 TDS 엔드포인트 항목에서 다음 2가지 설정을 관리합니다.

| 설정 | 기본값 | 역할 |
| --- | --- | --- |
| Enable TDS endpoint | 켜짐 | 환경의 TDS 연결 수신 여부 |
| Enable user level access control for TDS endpoint | 꺼짐 | 꺼짐: 환경의 모든 사용자가 TDS 엔드포인트 사용 가능 / 켜짐: Allow user to access TDS endpoint 권한을 가진 보안 역할의 사용자만 사용 가능 |

사용자 수준 액세스 제어를 켜면 Allow user to access TDS endpoint 권한이 없는 사용자는 `Attempted to perform an unauthorized operation. User misses prvAllowTDSAccess privilege` 오류를 받습니다. 이 권한은 보안 역할의 기타 권한(Miscellaneous privileges) 탭에서 조직 수준으로 부여하며, 이와 별도로 조회할 테이블의 데이터 액세스 권한도 필요합니다. 공식 문서는 데이터 유출 방지를 위해 이 설정을 켜고, 필요한 테이블만 조회할 수 있는 최소 권한 보안 역할을 부여하도록 권장합니다.

> **주의**
>
> Power BI의 Dataverse 커넥터, Excel 데이터 가져오기, Power Platform 데이터 흐름도 TDS 엔드포인트를 사용합니다. Enable TDS endpoint를 끄거나 사용자 수준 액세스 제어를 켜기 전에 이 기능들의 사용자에게 미치는 영향을 확인하십시오.

### 6.4 플러그 인 미적용

SQL 쿼리는 `RetrieveMultiple`, `Retrieve` 메시지에 등록된 플러그 인을 실행하지 않습니다. 플러그 인으로 쿼리나 조회 결과를 변경하는 환경에서는 앱 화면과 SQL 조회 결과가 다를 수 있습니다.

### 6.5 조회할 수 없는 테이블과 열

| 구분 | 대상 |
| --- | --- |
| 테이블 | 가상(virtual) 테이블, 감사(audit) 테이블, 탄력적(elastic) 테이블 |
| 열 형식 | binary, image, sql_variant, varbinary, virtual, HierarchyId, managedproperty, file, xml, partylist, timestamp, choices(다중 선택) |

partylist 열은 `activityparty` 테이블과 조인하여 조회할 수 있습니다.

## 7. .NET 10 샘플 코드

이 폴더에 .NET 10 파일 기반 앱(file-based app) 샘플 2개가 있습니다. 두 샘플은 최근 생성된 `account` 5건을 같은 조건으로 조회합니다.

| 파일 | 방식 | 인증 | 패키지 |
| --- | --- | --- | --- |
| `tds-sql.cs` | TDS 엔드포인트 + SQL | SqlClient의 Active Directory Interactive (Windows 로그인 창) | Microsoft.Data.SqlClient 7.1.1, Microsoft.Data.SqlClient.Extensions.Azure 7.1.1 |
| `odata.cs` | Web API (OData v4.0) | MSAL 브라우저 로그인 | Microsoft.Identity.Client 4.92.0 |

다음 명령으로 실행하십시오. `org00000000.crm5.dynamics.com`은 실제 환경의 호스트 이름으로 바꾸십시오. 인수를 생략하면 코드의 `host` 기본값을 사용하므로, 인수 없이 실행하려면 두 파일의 기본값을 먼저 바꾸십시오. 기본값이 예시 값 그대로이면 접속하지 않고 안내 메시지를 출력합니다.

```powershell
dotnet run tds-sql.cs -- org00000000.crm5.dynamics.com user@contoso.com
dotnet run odata.cs -- org00000000.crm5.dynamics.com
```

> **참고**
>
> - .NET 10 SDK가 필요합니다. `global.json`이 .NET 10 SDK를 지정하므로 상위 버전 SDK가 함께 설치된 PC에서도 .NET 10으로 실행됩니다.
> - SqlClient 7.x에서 Microsoft Entra 인증을 사용하려면 같은 버전의 Microsoft.Data.SqlClient.Extensions.Azure 패키지가 필요합니다.
> - `tds-sql.cs`는 SqlClient가 간접 참조하는 System.Security.Cryptography.Pkcs의 알려진 취약점을 피하기 위해 10.0.14를 직접 참조합니다.
> - `tds-sql.cs`의 로그인 창은 실행한 터미널 창을 부모 창으로 사용하므로 터미널에서 실행하십시오.
> - `odata.cs`의 클라이언트 ID(`51f81489-12ee-4a9e-aaae-a2591f45987d`)는 Microsoft Learn의 Dataverse 샘플 코드가 공용으로 사용하는 개발 및 테스트용 앱 등록입니다. 실제 앱에서는 Microsoft Entra ID에 직접 등록한 앱을 사용하십시오.

## 8. 테스트 환경

| 구성 요소 | 버전 |
| --- | --- |
| OS | Windows 11 Pro 10.0.26300 |
| sqlcmd (ODBC) | 15.0.1300.359 (ODBC Driver 17 for SQL Server 17.11.1 사용) |
| Visual Studio | Visual Studio 2026 |
| .NET SDK | 10.0.402 |

## 9. 참고 자료

- [Use SQL to query data (Microsoft Learn)](https://learn.microsoft.com/power-apps/developer/data-platform/dataverse-sql-query)
- [How Dataverse SQL differs from Transact-SQL (Microsoft Learn)](https://learn.microsoft.com/power-apps/developer/data-platform/how-dataverse-sql-differs-from-transact-sql)
- [Control access of the TDS endpoint (Microsoft Learn)](https://learn.microsoft.com/power-platform/admin/control-tds-settings)
- [Manage feature settings (Microsoft Learn)](https://learn.microsoft.com/power-platform/admin/settings-features)
- [Microsoft Entra authentication with Microsoft.Data.SqlClient (Microsoft Learn)](https://learn.microsoft.com/sql/connect/ado-net/sql/azure-active-directory-authentication)
- [Quick start: Web API sample (C#) (Microsoft Learn)](https://learn.microsoft.com/power-apps/developer/data-platform/webapi/quick-start-console-app-csharp)
