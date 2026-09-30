# NCAA Team Logos

This directory contains NCAA Division I FBS team logos sourced from the [College Football Data (CFBD) repository](https://github.com/CFBD/cfb-web).

## File Naming Convention

Logos are named using the CFBD team ID system: `{team_id}.png`

For example:
- `333.png` = Alabama
- `130.png` = Michigan
- `87.png` = Notre Dame

## Team Name to Logo Mapping

The `team-logo-mapping.json` file located at `wwwroot/team-logo-mapping.json` contains a complete mapping of team names to logo IDs for FBS and FCS teams (156 teams total).

### Duplicate Mappings Are Intentional

**Important:** The mapping file contains multiple entries for the same team with different name variations. This is intentional and beneficial!

Team names can appear in various formats depending on the data source:
- Full official names: "Michigan State"
- Common abbreviations: "MSU", "Mich State", "Mich St."
- Short names: "UConn" vs "Connecticut"
- Variations with punctuation: "Florida St" vs "Florida St."

Having duplicate mappings ensures that team logos are correctly displayed regardless of which name variation is provided by the data source. This is especially important when data comes from different sources like ESPN, CBS Sports, or other sports data providers that may use different naming conventions.

**Example duplicates:**
- "Michigan State", "Michigan St", "Michigan St.", "MSU", "Mich State", "Mich St", "Mich St." all map to ID "127"
- "Connecticut" and "UConn" both map to ID "41"
- "Florida State", "Florida St", "Florida St.", and "FSU" all map to ID "52"

Missing a team logo due to a name variation is bad. Having duplicate mappings is good!

Example usage in C#:
```csharp
// Read the mapping file
var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(
    File.ReadAllText("team-logo-mapping.json")
);

// Get logo URL for a team
var teamName = "Alabama";
var logoId = mapping[teamName]; // "333"
var logoUrl = $"/images/logos/ncaa/{logoId}.png";
```

Example usage in Blazor:
```razor
@if (teamLogoMapping.ContainsKey(game.Team))
{
    <img src="/images/logos/ncaa/@(teamLogoMapping[game.Team]).png" 
         alt="@game.Team logo" 
         class="team-logo" />
}
```

## Supported Teams

The application supports 156 NCAA Division I football teams, including both FBS and select FCS teams. This includes all teams that appear in weekly betting lines.

### All FBS Teams (2024-2025 Season)

| Team | Logo ID | Preview |
|------|---------|---------|
| Air Force | 2005 | ![](2005.png) |
| Akron | 2006 | ![](2006.png) |
| Alabama | 333 | ![](333.png) |
| Appalachian State | 2026 | ![](2026.png) |
| Arizona | 12 | ![](12.png) |
| Arizona State | 9 | ![](9.png) |
| Arkansas | 8 | ![](8.png) |
| Arkansas State | 2032 | ![](2032.png) |
| Army | 349 | ![](349.png) |
| Auburn | 2 | ![](2.png) |
| Ball State | 2050 | ![](2050.png) |
| Baylor | 239 | ![](239.png) |
| Boise State | 68 | ![](68.png) |
| Boston College | 103 | ![](103.png) |
| Bowling Green | 189 | ![](189.png) |
| Buffalo | 2084 | ![](2084.png) |
| BYU | 252 | ![](252.png) |
| California | 25 | ![](25.png) |
| Central Michigan | 2117 | ![](2117.png) |
| Charlotte | 2429 | ![](2429.png) |
| Cincinnati | 2132 | ![](2132.png) |
| Clemson | 228 | ![](228.png) |
| Coastal Carolina | 324 | ![](324.png) |
| Colorado | 38 | ![](38.png) |
| Colorado State | 36 | ![](36.png) |
| Connecticut | 41 | ![](41.png) |
| Duke | 150 | ![](150.png) |
| East Carolina | 151 | ![](151.png) |
| Eastern Michigan | 2199 | ![](2199.png) |
| Florida | 57 | ![](57.png) |
| Florida Atlantic | 2226 | ![](2226.png) |
| Florida International | 2229 | ![](2229.png) |
| Florida State | 52 | ![](52.png) |
| Fresno State | 278 | ![](278.png) |
| Georgia | 61 | ![](61.png) |
| Georgia Southern | 290 | ![](290.png) |
| Georgia State | 2247 | ![](2247.png) |
| Georgia Tech | 59 | ![](59.png) |
| Hawaii | 62 | ![](62.png) |
| Houston | 248 | ![](248.png) |
| Illinois | 356 | ![](356.png) |
| Indiana | 84 | ![](84.png) |
| Iowa | 2294 | ![](2294.png) |
| Iowa State | 66 | ![](66.png) |
| Jacksonville State | 55 | ![](55.png) |
| James Madison | 256 | ![](256.png) |
| Kansas | 2305 | ![](2305.png) |
| Kansas State | 2306 | ![](2306.png) |
| Kennesaw State | 338 | ![](338.png) |
| Kent State | 2309 | ![](2309.png) |
| Kentucky | 96 | ![](96.png) |
| Liberty | 2335 | ![](2335.png) |
| Louisiana | 309 | ![](309.png) |
| Louisiana Tech | 2348 | ![](2348.png) |
| Louisville | 97 | ![](97.png) |
| LSU | 99 | ![](99.png) |
| Marshall | 276 | ![](276.png) |
| Maryland | 120 | ![](120.png) |
| Memphis | 235 | ![](235.png) |
| Miami | 2390 | ![](2390.png) |
| Miami (OH) | 193 | ![](193.png) |
| Michigan | 130 | ![](130.png) |
| Michigan State | 127 | ![](127.png) |
| Middle Tennessee | 2393 | ![](2393.png) |
| Minnesota | 135 | ![](135.png) |
| Mississippi State | 344 | ![](344.png) |
| Missouri | 142 | ![](142.png) |
| Navy | 2426 | ![](2426.png) |
| NC State | 152 | ![](152.png) |
| Nebraska | 158 | ![](158.png) |
| Nevada | 2440 | ![](2440.png) |
| New Mexico | 167 | ![](167.png) |
| New Mexico State | 166 | ![](166.png) |
| North Carolina | 153 | ![](153.png) |
| North Texas | 249 | ![](249.png) |
| Northern Illinois | 2459 | ![](2459.png) |
| Northwestern | 77 | ![](77.png) |
| Notre Dame | 87 | ![](87.png) |
| Ohio | 195 | ![](195.png) |
| Ohio State | 194 | ![](194.png) |
| Oklahoma | 201 | ![](201.png) |
| Oklahoma State | 197 | ![](197.png) |
| Old Dominion | 295 | ![](295.png) |
| Ole Miss | 145 | ![](145.png) |
| Oregon | 2483 | ![](2483.png) |
| Oregon State | 204 | ![](204.png) |
| Penn State | 213 | ![](213.png) |
| Pittsburgh | 221 | ![](221.png) |
| Purdue | 2509 | ![](2509.png) |
| Rice | 242 | ![](242.png) |
| Rutgers | 164 | ![](164.png) |
| Sam Houston | 2534 | ![](2534.png) |
| San Diego State | 21 | ![](21.png) |
| San Jose State | 23 | ![](23.png) |
| SMU | 2567 | ![](2567.png) |
| South Alabama | 6 | ![](6.png) |
| South Carolina | 2579 | ![](2579.png) |
| South Florida | 58 | ![](58.png) |
| Southern Miss | 2608 | ![](2608.png) |
| Stanford | 24 | ![](24.png) |
| Syracuse | 183 | ![](183.png) |
| TCU | 2628 | ![](2628.png) |
| Temple | 218 | ![](218.png) |
| Tennessee | 2633 | ![](2633.png) |
| Texas | 251 | ![](251.png) |
| Texas A&M | 245 | ![](245.png) |
| Texas State | 326 | ![](326.png) |
| Texas Tech | 2641 | ![](2641.png) |
| Toledo | 2649 | ![](2649.png) |
| Troy | 2653 | ![](2653.png) |
| Tulane | 2655 | ![](2655.png) |
| Tulsa | 202 | ![](202.png) |
| UAB | 5 | ![](5.png) |
| UCF | 2116 | ![](2116.png) |
| UCLA | 26 | ![](26.png) |
| UL Monroe | 2433 | ![](2433.png) |
| UMass | 113 | ![](113.png) |
| UNLV | 2439 | ![](2439.png) |
| USC | 30 | ![](30.png) |
| Utah | 254 | ![](254.png) |
| Utah State | 328 | ![](328.png) |
| UTEP | 2638 | ![](2638.png) |
| UTSA | 2636 | ![](2636.png) |
| Vanderbilt | 238 | ![](238.png) |
| Virginia | 258 | ![](258.png) |
| Virginia Tech | 259 | ![](259.png) |
| Wake Forest | 154 | ![](154.png) |
| Washington | 264 | ![](264.png) |
| Washington State | 265 | ![](265.png) |
| West Virginia | 277 | ![](277.png) |
| Western Kentucky | 98 | ![](98.png) |
| Western Michigan | 2711 | ![](2711.png) |
| Wisconsin | 275 | ![](275.png) |
| Wyoming | 2770 | ![](2770.png) |

### FBS Teams Added (2025)

The following FBS teams have been added to support games that appear in weekly betting lines:

| Team | Logo ID |
|------|---------|
| Kennesaw State | 338 |
| UL Lafayette | 309 |
| UTEP | 2638 |
| UTSA | 2636 |

### FCS Teams Added (2025)

The following FCS teams have been added to support games that appear in weekly betting lines:

| Team | Logo ID |
|------|---------|
| Abilene Christian | 2000 |
| Albany | 399 |
| Bryant | 2806 |
| Central Arkansas | 2110 |
| Delaware | 48 |
| Delaware State | 2169 |
| Eastern Kentucky | 2198 |
| Elon | 2210 |
| Idaho | 70 |
| Lamar | 2320 |
| Merrimack | 2620 |
| Missouri State | 2623 |
| North Arizona | 2464 |
| North Dakota | 2551 |
| Portland State | 2502 |
| SE Louisiana | 2524 |
| SF Austin | 2612 |
| St Francis PA | 2545 |
| Stony Brook | 2615 |
| UT Martin | 2632 |

## Team Colors

Team colors are maintained in a separate `team-color-mapping.json` file at `wwwroot/team-color-mapping.json`. Each team has a primary and secondary color defined as 6-digit hexadecimal color codes with a '#' prefix (e.g., #RRGGBB).

Example:
```json
{
  "Alabama": { "primary": "#9E1B32", "secondary": "#828A8F" },
  "Abilene Christian": { "primary": "#4F2170", "secondary": "#FFFFFF" }
}
```

## License

These logos are sourced from the CFBD project which is licensed under MIT License.
The logos themselves belong to their respective universities and athletic programs.

## Credits

Logos provided by [CollegeFootballData.com](https://collegefootballdata.com/)
