using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JuegoTCG.Cards
{
    /// <summary>
    /// Base de datos y utilidad para mapear nombres de países (en español, inglés y alias comunes)
    /// a sus códigos de bandera oficiales según el estándar ISO 3166-1 alpha-2 utilizado por Flagpedia.
    /// </summary>
    public static class CountryCodeHelper
    {
        // Mapeo de nombres normalizados -> Código ISO 2 letras (Flagpedia)
        private static readonly Dictionary<string, string> NameToCodeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Mapeo de Código ISO -> Nombre canónico en español
        private static readonly Dictionary<string, string> CodeToNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Set de todos los códigos ISO válidos
        private static readonly HashSet<string> ValidIsoCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static CountryCodeHelper()
        {
            InitializeDatabase();
        }

        /// <summary>
        /// Normaliza una cadena quitando tildes, símbolos de puntuación, espacios extras y pasando a minúsculas.
        /// </summary>
        public static string NormalizeCountryName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string text = input.Trim().ToLowerInvariant();
            text = RemoveDiacritics(text);

            // Reemplazar signos de puntuación comunes por espacios
            text = text.Replace(".", " ")
                       .Replace("-", " ")
                       .Replace("_", " ")
                       .Replace(",", " ")
                       .Replace("'", "")
                       .Replace("`", "");

            // Reducir espacios múltiples a uno solo
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            return text.Trim();
        }

        private static string RemoveDiacritics(string text)
        {
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            for (int i = 0; i < normalized.Length; i++)
            {
                char c = normalized[i];
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Intenta obtener el código ISO / Flagpedia (2 letras) para un país dado.
        /// Acepta nombres en español, inglés, alias de fútbol o el código ISO directo.
        /// </summary>
        public static bool TryGetCode(string countryNameOrCode, out string code)
        {
            code = null;
            if (string.IsNullOrWhiteSpace(countryNameOrCode)) return false;

            string trimmed = countryNameOrCode.Trim();

            // Si ya es un código ISO válido de 2 letras (ej: "ES", "AR", "CO")
            if (trimmed.Length == 2 && ValidIsoCodes.Contains(trimmed))
            {
                code = trimmed.ToUpperInvariant();
                return true;
            }

            string normalized = NormalizeCountryName(trimmed);

            // Búsqueda directa en el diccionario normalizado
            if (NameToCodeMap.TryGetValue(normalized, out code))
            {
                return true;
            }

            // Búsqueda por coincidencia parcial si tiene al menos 4 letras
            // Ej: "República Argentina" -> contiene "argentina" -> "AR"
            foreach (var kvp in NameToCodeMap)
            {
                if (kvp.Key.Length >= 4 && (normalized.Contains(kvp.Key) || kvp.Key.Contains(normalized)))
                {
                    code = kvp.Value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Obtiene el código ISO / Flagpedia correspondiente o un valor por defecto si no se encuentra.
        /// </summary>
        public static string GetCountryCode(string countryNameOrCode, string defaultCode = "ES")
        {
            return TryGetCode(countryNameOrCode, out string code) ? code : defaultCode;
        }

        /// <summary>
        /// Obtiene el nombre canónico en español a partir del código de bandera (ej: "ES" -> "España").
        /// </summary>
        public static string GetCountryName(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;
            string cleanCode = code.Trim().ToUpperInvariant();
            return CodeToNameMap.TryGetValue(cleanCode, out string name) ? name : cleanCode;
        }

        /// <summary>
        /// Indica si un código dado de 2 caracteres es un código ISO reconocido.
        /// </summary>
        public static bool IsValidCode(string code)
        {
            return !string.IsNullOrWhiteSpace(code) && ValidIsoCodes.Contains(code.Trim());
        }

        private static void Register(string code, string spanishCanonical, string[] aliases)
        {
            code = code.ToUpperInvariant();
            ValidIsoCodes.Add(code);

            if (!CodeToNameMap.ContainsKey(code))
            {
                CodeToNameMap[code] = spanishCanonical;
            }

            RegisterName(spanishCanonical, code);

            if (aliases != null)
            {
                for (int i = 0; i < aliases.Length; i++)
                {
                    RegisterName(aliases[i], code);
                }
            }
        }

        private static void RegisterName(string name, string code)
        {
            string norm = NormalizeCountryName(name);
            if (!string.IsNullOrEmpty(norm) && !NameToCodeMap.ContainsKey(norm))
            {
                NameToCodeMap[norm] = code;
            }
        }

        private static void InitializeDatabase()
        {
            // =========================================================================
            // BASE DE DATOS COMPLETA DE CÓDIGOS DE BANDERA FLAGPEDIA / ISO 3166-1 ALPHA-2
            // Incluye los 195+ países del mundo, territorios y aliases de fútbol
            // =========================================================================

            // --- A ---
            Register("AD", "Andorra", new[] { "Principado de Andorra" });
            Register("AE", "Emiratos Árabes Unidos", new[] { "Emiratos Arabes", "UAE", "EAU", "United Arab Emirates", "Dubai", "Abu Dhabi" });
            Register("AF", "Afganistán", new[] { "Afganistan", "Afghanistan" });
            Register("AG", "Antigua y Barbuda", new[] { "Antigua and Barbuda", "Antigua" });
            Register("AI", "Anguila", new[] { "Anguilla" });
            Register("AL", "Albania", new[] { "Shqiperia" });
            Register("AM", "Armenia", new[] { "Hayastan" });
            Register("AO", "Angola", null);
            Register("AQ", "Antártida", new[] { "Antartida", "Antarctica" });
            Register("AR", "Argentina", new[] { "República Argentina", "Republica Argentina", "Arg" });
            Register("AS", "Samoa Americana", new[] { "American Samoa" });
            Register("AT", "Austria", new[] { "Österreich", "Osterreich", "AUT" });
            Register("AU", "Australia", new[] { "AUS" });
            Register("AW", "Aruba", null);
            Register("AX", "Islas Åland", new[] { "Aland", "Aland Islands", "Islas Aland" });
            Register("AZ", "Azerbaiyán", new[] { "Azerbaiyan", "Azerbaijan" });

            // --- B ---
            Register("BA", "Bosnia y Herzegovina", new[] { "Bosnia Herzegovina", "Bosnia", "Bosnia & Herzegovina" });
            Register("BB", "Barbados", null);
            Register("BD", "Bangladés", new[] { "Bangladesh", "Banglades" });
            Register("BE", "Bélgica", new[] { "Belgica", "Belgium", "Belgique", "Belgie", "BEL" });
            Register("BF", "Burkina Faso", null);
            Register("BG", "Bulgaria", new[] { "BUL" });
            Register("BH", "Baréin", new[] { "Barein", "Bahrein", "Bahrain" });
            Register("BI", "Burundi", null);
            Register("BJ", "Benín", new[] { "Benin" });
            Register("BL", "San Bartolomé", new[] { "Saint Barthelemy", "St Barts" });
            Register("BM", "Bermudas", new[] { "Bermuda" });
            Register("BN", "Brunéi", new[] { "Brunei", "Brunei Darussalam" });
            Register("BO", "Bolivia", new[] { "Estado Plurinacional de Bolivia", "BOL" });
            Register("BQ", "Caribe Neerlandés", new[] { "Bonaire", "Bonaire Sint Eustatius and Saba" });
            Register("BR", "Brasil", new[] { "Brazil", "BRA" });
            Register("BS", "Bahamas", new[] { "The Bahamas" });
            Register("BT", "Bután", new[] { "Butan", "Bhutan" });
            Register("BV", "Isla Bouvet", new[] { "Bouvet Island" });
            Register("BW", "Botsuana", new[] { "Botswana" });
            Register("BY", "Bielorrusia", new[] { "Belarus" });
            Register("BZ", "Belice", new[] { "Belize" });

            // --- C ---
            Register("CA", "Canadá", new[] { "Canada", "CAN" });
            Register("CC", "Islas Cocos", new[] { "Cocos Islands", "Keeling Islands" });
            Register("CD", "República Democrática del Congo", new[] { "RD Congo", "RDC", "DR Congo", "Democratic Republic of the Congo", "Congo Kinshasa", "Zaire" });
            Register("CF", "República Centroafricana", new[] { "Republica Centroafricana", "Central African Republic" });
            Register("CG", "República del Congo", new[] { "Republica del Congo", "Congo", "Congo Brazzaville", "Republic of the Congo" });
            Register("CH", "Suiza", new[] { "Switzerland", "Schweiz", "Suisse", "Svizzera", "SUI" });
            Register("CI", "Costa de Marfil", new[] { "Ivory Coast", "Cote d Ivoire", "Cote d'Ivoire", "CIV" });
            Register("CK", "Islas Cook", new[] { "Cook Islands" });
            Register("CL", "Chile", new[] { "CHI" });
            Register("CM", "Camerún", new[] { "Camerun", "Cameroon", "CMR" });
            Register("CN", "China", new[] { "República Popular China", "PRC" });
            Register("CO", "Colombia", new[] { "República de Colombia", "COL" });
            Register("CR", "Costa Rica", new[] { "CRC" });
            Register("CU", "Cuba", new[] { "CUB" });
            Register("CV", "Cabo Verde", new[] { "Cape Verde" });
            Register("CW", "Curazao", new[] { "Curacao" });
            Register("CX", "Isla de Navidad", new[] { "Christmas Island" });
            Register("CY", "Chipre", new[] { "Cyprus" });
            Register("CZ", "Chequia", new[] { "República Checa", "Republica Checa", "Czech Republic", "Czechia", "CZE" });

            // --- D ---
            Register("DE", "Alemania", new[] { "Germany", "Deutschland", "GER", "DEU" });
            Register("DJ", "Yibuti", new[] { "Djibouti" });
            Register("DK", "Dinamarca", new[] { "Denmark", "Danmark", "DEN" });
            Register("DM", "Dominica", null);
            Register("DO", "República Dominicana", new[] { "Republica Dominicana", "Dominican Republic", "DOM" });
            Register("DZ", "Argelia", new[] { "Algeria", "Algérie", "ALG" });

            // --- E ---
            Register("EC", "Ecuador", new[] { "ECU" });
            Register("EE", "Estonia", new[] { "Eesti", "EST" });
            Register("EG", "Egipto", new[] { "Egypt", "EGY" });
            Register("EH", "Sahara Occidental", new[] { "Western Sahara" });
            Register("ER", "Eritrea", null);
            Register("ES", "España", new[] { "Espana", "Spain", "ESP" });
            Register("ET", "Etiopía", new[] { "Etiopia", "Ethiopia", "ETH" });

            // --- F ---
            Register("FI", "Finlandia", new[] { "Finland", "Suomi", "FIN" });
            Register("FJ", "Fiyi", new[] { "Fiji" });
            Register("FK", "Islas Malvinas", new[] { "Falkland Islands", "Malvinas" });
            Register("FM", "Micronesia", new[] { "Federated States of Micronesia" });
            Register("FO", "Islas Feroe", new[] { "Faroe Islands", "Feroe" });
            Register("FR", "Francia", new[] { "France", "FRA" });

            // --- G ---
            Register("GA", "Gabón", new[] { "Gabon", "GAB" });
            Register("GB", "Reino Unido", new[] { "Gran Bretaña", "Gran Bretana", "United Kingdom", "Great Britain", "UK", "Inglaterra", "England", "Escocia", "Scotland", "Gales", "Wales", "Irlanda del Norte", "Northern Ireland", "ENG", "SCO", "WAL", "NIR" });
            Register("GD", "Granada", new[] { "Grenada" });
            Register("GE", "Georgia", new[] { "GEO" });
            Register("GF", "Guayana Francesa", new[] { "French Guiana" });
            Register("GG", "Guernsey", null);
            Register("GH", "Ghana", new[] { "GHA" });
            Register("GI", "Gibraltar", null);
            Register("GL", "Groenlandia", new[] { "Greenland" });
            Register("GM", "Gambia", new[] { "The Gambia" });
            Register("GN", "Guinea", new[] { "Guinea Conakry", "GUI" });
            Register("GP", "Guadalupe", new[] { "Guadeloupe" });
            Register("GQ", "Guinea Ecuatorial", new[] { "Equatorial Guinea", "EQG" });
            Register("GR", "Grecia", new[] { "Greece", "Hellas", "GRE" });
            Register("GS", "Islas Georgias del Sur", new[] { "South Georgia", "South Sandwich Islands" });
            Register("GT", "Guatemala", new[] { "GUA" });
            Register("GU", "Guam", null);
            Register("GW", "Guinea-Bisáu", new[] { "Guinea Bisau", "Guinea-Bissau", "Guinea Bissau", "GBS" });
            Register("GY", "Guyana", null);

            // --- H ---
            Register("HK", "Hong Kong", new[] { "HKG" });
            Register("HM", "Islas Heard y McDonald", new[] { "Heard and McDonald Islands" });
            Register("HN", "Honduras", new[] { "HON" });
            Register("HR", "Croacia", new[] { "Croatia", "Hrvatska", "CRO" });
            Register("HT", "Haití", new[] { "Haiti", "HAI" });
            Register("HU", "Hungría", new[] { "Hungria", "Hungary", "Magyarorszag", "HUN" });

            // --- I ---
            Register("ID", "Indonesia", new[] { "INA" });
            Register("IE", "Irlanda", new[] { "Ireland", "Republic of Ireland", "Eire", "IRL" });
            Register("IL", "Israel", new[] { "ISR" });
            Register("IM", "Isla de Man", new[] { "Isle of Man" });
            Register("IN", "India", new[] { "IND" });
            Register("IO", "Territorio Británico del Océano Índico", new[] { "British Indian Ocean Territory" });
            Register("IQ", "Irak", new[] { "Iraq", "IRQ" });
            Register("IR", "Irán", new[] { "Iran", "IRN" });
            Register("IS", "Islandia", new[] { "Iceland", "ISL" });
            Register("IT", "Italia", new[] { "Italy", "ITA" });

            // --- J ---
            Register("JE", "Jersey", null);
            Register("JM", "Jamaica", new[] { "JAM" });
            Register("JO", "Jordania", new[] { "Jordan", "JOR" });
            Register("JP", "Japón", new[] { "Japon", "Japan", "Nippon", "JPN" });

            // --- K ---
            Register("KE", "Kenia", new[] { "Kenya", "KEN" });
            Register("KG", "Kirguistán", new[] { "Kirguistan", "Kyrgyzstan", "KGZ" });
            Register("KH", "Camboya", new[] { "Cambodia", "CAM" });
            Register("KI", "Kiribati", null);
            Register("KM", "Comoras", new[] { "Comoros" });
            Register("KN", "San Cristóbal y Nieves", new[] { "San Cristobal y Nieves", "Saint Kitts and Nevis", "St Kitts" });
            Register("KP", "Corea del Norte", new[] { "North Korea", "DPRK" });
            Register("KR", "Corea del Sur", new[] { "Corea", "South Korea", "Korea", "Republic of Korea", "KOR" });
            Register("KW", "Kuwait", new[] { "KUW" });
            Register("KY", "Islas Caimán", new[] { "Islas Caiman", "Cayman Islands" });
            Register("KZ", "Kazajistán", new[] { "Kazajistan", "Kazakhstan", "KAZ" });

            // --- L ---
            Register("LA", "Laos", new[] { "Lao" });
            Register("LB", "Líbano", new[] { "Libano", "Lebanon", "LIB" });
            Register("LC", "Santa Lucía", new[] { "Santa Lucia", "Saint Lucia" });
            Register("LI", "Liechtenstein", new[] { "LIE" });
            Register("LK", "Sri Lanka", new[] { "SRI" });
            Register("LR", "Liberia", new[] { "LBR" });
            Register("LS", "Lesoto", new[] { "Lesotho" });
            Register("LT", "Lituania", new[] { "Lithuania", "Lietuva", "LTU" });
            Register("LU", "Luxemburgo", new[] { "Luxembourg", "LUX" });
            Register("LV", "Letonia", new[] { "Latvia", "Latvija", "LVA" });
            Register("LY", "Libia", new[] { "Libya", "LBY" });

            // --- M ---
            Register("MA", "Marruecos", new[] { "Morocco", "Maroc", "MAR" });
            Register("MC", "Mónaco", new[] { "Monaco" });
            Register("MD", "Moldavia", new[] { "Moldova", "MDA" });
            Register("ME", "Montenegro", new[] { "MNE" });
            Register("MF", "San Martín (Francia)", new[] { "Saint Martin" });
            Register("MG", "Madagascar", new[] { "MAD" });
            Register("MH", "Islas Marshall", new[] { "Marshall Islands" });
            Register("MK", "Macedonia del Norte", new[] { "Macedonia", "North Macedonia", "MKD" });
            Register("ML", "Malí", new[] { "Mali", "MLI" });
            Register("MM", "Birmania", new[] { "Myanmar", "MYA" });
            Register("MN", "Mongolia", new[] { "MGL" });
            Register("MO", "Macao", new[] { "Macau" });
            Register("MP", "Islas Marianas del Norte", new[] { "Northern Mariana Islands" });
            Register("MQ", "Martinica", new[] { "Martinique" });
            Register("MR", "Mauritania", new[] { "MTN" });
            Register("MS", "Montserrat", null);
            Register("MT", "Malta", new[] { "MLT" });
            Register("MU", "Mauricio", new[] { "Mauritius", "MRI" });
            Register("MV", "Maldivas", new[] { "Maldives", "MDV" });
            Register("MW", "Malaui", new[] { "Malawi", "MWI" });
            Register("MX", "México", new[] { "Mexico", "MEX" });
            Register("MY", "Malasia", new[] { "Malaysia", "MAS" });
            Register("MZ", "Mozambique", new[] { "MOZ" });

            // --- N ---
            Register("NA", "Namibia", new[] { "NAM" });
            Register("NC", "Nueva Caledonia", new[] { "New Caledonia" });
            Register("NE", "Níger", new[] { "Niger", "NIG" });
            Register("NF", "Isla Norfolk", new[] { "Norfolk Island" });
            Register("NG", "Nigeria", new[] { "NGA" });
            Register("NI", "Nicaragua", new[] { "NCA" });
            Register("NL", "Países Bajos", new[] { "Paises Bajos", "Holanda", "Netherlands", "Holland", "Nederland", "NED" });
            Register("NO", "Noruega", new[] { "Norway", "Norge", "NOR" });
            Register("NP", "Nepal", new[] { "NEP" });
            Register("NR", "Nauru", null);
            Register("NU", "Niue", null);
            Register("NZ", "Nueva Zelanda", new[] { "Nueva Zelandia", "New Zealand", "Aotearoa", "NZL" });

            // --- O ---
            Register("OM", "Omán", new[] { "Oman", "OMA" });

            // --- P ---
            Register("PA", "Panamá", new[] { "Panama", "PAN" });
            Register("PE", "Perú", new[] { "Peru", "PER" });
            Register("PF", "Polinesia Francesa", new[] { "French Polynesia", "Tahiti" });
            Register("PG", "Papúa Nueva Guinea", new[] { "Papua Nueva Guinea", "Papua New Guinea", "PNG" });
            Register("PH", "Filipinas", new[] { "Philippines", "PHI" });
            Register("PK", "Pakistán", new[] { "Pakistan", "PAK" });
            Register("PL", "Polonia", new[] { "Poland", "Polska", "POL" });
            Register("PM", "San Pedro y Miquelón", new[] { "Saint Pierre and Miquelon" });
            Register("PN", "Islas Pitcairn", new[] { "Pitcairn Islands" });
            Register("PR", "Puerto Rico", new[] { "PUR" });
            Register("PS", "Palestina", new[] { "Palestine", "PLE" });
            Register("PT", "Portugal", new[] { "POR" });
            Register("PW", "Palaos", new[] { "Palau" });
            Register("PY", "Paraguay", new[] { "PAR" });

            // --- Q ---
            Register("QA", "Catar", new[] { "Qatar", "QAT" });

            // --- R ---
            Register("RE", "Reunión", new[] { "Reunion" });
            Register("RO", "Rumanía", new[] { "Rumania", "Romania", "ROU" });
            Register("RS", "Serbia", new[] { "SRB" });
            Register("RU", "Rusia", new[] { "Russia", "RUS" });
            Register("RW", "Ruanda", new[] { "Rwanda", "RWA" });

            // --- S ---
            Register("SA", "Arabia Saudí", new[] { "Arabia Saudita", "Saudi Arabia", "KSA" });
            Register("SB", "Islas Salomón", new[] { "Islas Salomon", "Solomon Islands" });
            Register("SC", "Seychelles", new[] { "SEY" });
            Register("SD", "Sudán", new[] { "Sudan", "SUD" });
            Register("SE", "Suecia", new[] { "Sweden", "Sverige", "SWE" });
            Register("SG", "Singapur", new[] { "Singapore", "SIN" });
            Register("SH", "Santa Elena", new[] { "Saint Helena" });
            Register("SI", "Eslovenia", new[] { "Slovenia", "Slovenija", "SVN" });
            Register("SJ", "Svalbard y Jan Mayen", new[] { "Svalbard" });
            Register("SK", "Eslovaquia", new[] { "Slovakia", "Slovensko", "SVK" });
            Register("SL", "Sierra Leona", new[] { "Sierra Leone", "SLE" });
            Register("SM", "San Marino", new[] { "SMR" });
            Register("SN", "Senegal", new[] { "SEN" });
            Register("SO", "Somalia", new[] { "SOM" });
            Register("SR", "Surinam", new[] { "Suriname", "SUR" });
            Register("SS", "Sudán del Sur", new[] { "Sudan del Sur", "South Sudan", "SSD" });
            Register("ST", "Santo Tomé y Príncipe", new[] { "Santo Tome y Principe", "Sao Tome and Principe" });
            Register("SV", "El Salvador", new[] { "SLV" });
            Register("SX", "San Martín (Países Bajos)", new[] { "Sint Maarten" });
            Register("SY", "Siria", new[] { "Syria", "SYR" });
            Register("SZ", "Esuatini", new[] { "Suazilandia", "Eswatini", "Swaziland", "SWZ" });

            // --- T ---
            Register("TC", "Islas Turcas y Caicos", new[] { "Turks and Caicos Islands" });
            Register("TD", "Chad", new[] { "CHA" });
            Register("TF", "Tierras Australes Francesas", new[] { "French Southern Territories" });
            Register("TG", "Togo", new[] { "TOG" });
            Register("TH", "Tailandia", new[] { "Thailand", "THA" });
            Register("TJ", "Tayikistán", new[] { "Tayikistan", "Tajikistan", "TJK" });
            Register("TK", "Tokelau", null);
            Register("TL", "Timor Oriental", new[] { "Timor Leste", "East Timor", "TLS" });
            Register("TM", "Turkmenistán", new[] { "Turkmenistan", "TKM" });
            Register("TN", "Túnez", new[] { "Tunez", "Tunisia", "TUN" });
            Register("TO", "Tonga", new[] { "TGA" });
            Register("TR", "Turquía", new[] { "Turquia", "Turkey", "Türkiye", "Turkiye", "TUR" });
            Register("TT", "Trinidad y Tobago", new[] { "Trinidad and Tobago", "TRI" });
            Register("TV", "Tuvalu", null);
            Register("TW", "Taiwán", new[] { "Taiwan", "Chinese Taipei" });
            Register("TZ", "Tanzania", new[] { "TAN" });

            // --- U ---
            Register("UA", "Ucrania", new[] { "Ukraine", "UKR" });
            Register("UG", "Uganda", new[] { "UGA" });
            Register("UM", "Islas Ultramarinas Menores de EE. UU.", new[] { "United States Minor Outlying Islands" });
            Register("US", "Estados Unidos", new[] { "EEUU", "EE.UU.", "USA", "United States", "United States of America", "U.S.A." });
            Register("UY", "Uruguay", new[] { "República Oriental del Uruguay", "URU" });
            Register("UZ", "Uzbekistán", new[] { "Uzbekistan", "UZB" });

            // --- V ---
            Register("VA", "Ciudad del Vaticano", new[] { "Vaticano", "Vatican City", "Holy See" });
            Register("VC", "San Vicente y las Granadinas", new[] { "Saint Vincent and the Grenadines", "St Vincent" });
            Register("VE", "Venezuela", new[] { "República Bolivariana de Venezuela", "VEN" });
            Register("VG", "Islas Vírgenes Británicas", new[] { "Islas Virgenes Britanicas", "British Virgin Islands", "BVI" });
            Register("VI", "Islas Vírgenes de EE. UU.", new[] { "Islas Virgenes de los Estados Unidos", "U.S. Virgin Islands", "US Virgin Islands" });
            Register("VN", "Vietnam", new[] { "VIE" });
            Register("VU", "Vanuatu", new[] { "VAN" });

            // --- W ---
            Register("WF", "Wallis y Futuna", new[] { "Wallis and Futuna" });
            Register("WS", "Samoa", new[] { "SAM" });

            // --- X ---
            Register("XK", "Kosovo", new[] { "KOS" });

            // --- Y ---
            Register("YE", "Yemen", new[] { "YEM" });
            Register("YT", "Mayotte", null);

            // --- Z ---
            Register("ZA", "Sudáfrica", new[] { "Sudafrica", "South Africa", "RSA" });
            Register("ZM", "Zambia", new[] { "ZAM" });
            Register("ZW", "Zimbabue", new[] { "Zimbabwe", "ZIM" });
        }
    }
}
