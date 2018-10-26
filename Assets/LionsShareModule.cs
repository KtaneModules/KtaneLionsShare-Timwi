using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KModkit;
using LionsShare;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Lion’s Share
/// Created by Timwi
/// </summary>
public class LionsShareModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public KMColorblindMode ColorblindMode;

    public Mesh[] PieSliceMeshes;
    public MeshRenderer[] PieSlices;
    public MeshFilter[] PieMeshFilters;
    public KMSelectable[] LionSelectables;
    public TextMesh[] LionNameMeshes;
    public KMSelectable ButtonInc;
    public KMSelectable ButtonDec;
    public KMSelectable ButtonSubmit;
    public TextMesh Year;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _isSolved;
    private Color[] _unselectedPieSliceColors;
    private Color[] _selectedPieSliceColors;
    private string[] _lionNames;
    private int[] _currentPortions;
    private int[] _correctPortions;
    private int _leadHuntressIx;
    private int _selectedLion;
    private float _startAngle;
    private bool _colorblindEnabled;

    enum LionStatus
    {
        Null,
        Unborn,
        Cub,
        Adult,
        Absent,
        King,
        Dead,
        Visiting
    }

    enum IndicatorRule { Any, Lit, Unlit }

    sealed class Lion
    {
        public string Name;
        public string Mother;
        public bool Male;
        public LionStatus[] Status;
    }

    // FOR SEED 1
    private static readonly KeyValuePair<string, bool>[] _visitingLionNames = "Tojo,m;Chumvi,m;Malka,m;Askari,m;Tama,f;Rani,f;Zuri,f;Tiifu,f;Kula,f;Naanda,f;Ndona,f;Sheena,f;Diku,f;Boga,f;Sabini,f;Babu,f;Weena,f"
        .Split(';').Select(str => str.Split(','))
        .Select(arr => new KeyValuePair<string, bool>(arr[0], arr[1] == "m"))
        .ToArray();

    private static readonly Lion[] _allLions = @"Taka,m,Uru,01223555555
Mufasa,m,Uru,12235
Uru,f,,333333
Ahadi,m,,3355
Zama,f,,333
Mohatu,m,,55
Kion,m,Nala,0000000000001225
Kiara,f,Nala,0000000000012233
Kopa,m,Nala,0000000000012
Kovu,m,Zira,0000000000122333
Vitani,f,Zira,0000000001223333
Nuka,m,Zira,0000000012233
Mheetu,m,Sarabi,0000012233344433
Zira,f,Sarafina,0000012233333
Nala,f,Sarafina,0000122333433333
Simba,m,Sarabi,000124444445555
Sarabi,f,,22333333333333
Sarafina,f,,223333333333"
        .Replace("\r", "").Split('\n').Select(str => str.Split(','))
        .Select(arr => new Lion { Name = arr[0], Male = arr[1] == "m", Mother = arr[2], Status = arr[3].PadRight(16, '6').Select(ch => (LionStatus) (ch - '0')).ToArray() }).ToArray();

    // FOR ALL OTHER RULE SEEDS
    private static readonly string[] _maleNames = new[] { "Abasi", "Abdu", "Adhama", "Amani", "Anasa", "Ashon", "Atieno", "Ayo", "Azima", "Azizi", "Badru", "Baraka", "Barasa", "Bayana", "Beno", "Busar", "Busara", "Bwana", "Chane", "Daudi", "Duma", "Elea", "Eli", "Fanaka", "Faraji", "Hali", "Hamadi", "Hamidi", "Hamisi", "Hodari", "Ikeno", "Jafari", "Jamba", "Jata", "Jela", "Juma", "Jumaane", "Kamari", "Kiama", "Kifeda", "Kiira", "Kito", "Kitwana", "Kobe", "Koman", "Kuende", "Maulidi", "Mbita", "Mhina", "Mosi", "Moyo", "Mpenda", "Mshindi", "Msia", "Murati", "Musa", "Mwinyi", "Mzuzi", "Neema", "Njogwa", "Nyo", "Rajabu", "Safiri", "Salehe", "Salene", "Sefu", "Shani", "Shomari", "Tabia", "Taji", "Tambo", "Tamu", "Tian", "Umoja", "Usian", "Yahya", "Zahur", "Zakia", "Zawadi", "Abelo", "Amahle", "Amehlo", "Andile", "Anele", "Anelisa", "Avela", "Ayanda", "Ayize", "Azisa", "Bafana", "Bandile", "Bheka", "Bhekifa", "Bongani", "Busiso", "Cakaza", "Cashile", "Cebo", "Cela", "Chacha", "Chaka", "Chakide", "Chipo", "Chotho", "Chweba", "Ciko", "Cilonga", "Cothoza", "Cwatha", "Dabu", "Dabuka", "Daza", "Delani", "Dida", "Dinfa", "Dingane", "Dingani", "Donya", "Dube", "Duma", "Dumisa", "Dumo", "Duna", "Dundu", "Eethaba", "Elethu", "Falakhe", "Fanyana", "Fikile", "Fobela", "Fokazi", "Fowabo", "Fowenu", "Fudu", "Fundani", "Fundisa", "Fuza", "Fuzo", "Gabadi", "Gangi", "Gania", "Ganya", "Gatsha", "Gazi", "Gazini", "Gebhuza", "Geza", "Gibeli", "Gijimi", "Goduka", "Guduza", "Gugu", "Gwala", "Gwazi", "Gwedi", "Gwili", "Hlelile", "Ibubesi", "Ifu", "Igama", "Ikhanda", "Impela", "Impi", "Impisi", "Indlovu", "Induna", "Ingane", "Ingwe", "Inyoni", "Iqaqa", "Iqhunde", "Izagala", "Izula", "Jabu", "Jama", "Kgabu", "Khwezi", "Khulani", "Kubu", "Kufika", "Kwanele", "Kwazulu", "Lethu", "Lindani", "Lindela", "Lindile", "Litha", "Lizwi", "Lunga", "Lungelo", "Lungile", "Luvo", "Luyanda", "Lwazi", "Makhaya", "Malusi", "Manqoba", "Mapoza", "Mbube", "Mcebo", "Mduduzi", "Menzi", "Mfundo", "Mhambi", "Mmeli", "Mndeni", "Mnotho", "Mnqobi", "Mondli", "Mpilo", "Mpucuko", "Msizi", "Mthunzi", "Musa", "Mvelo", "Mzamo", "Ndonsa", "Nduduzo", "Ndumiso", "Nhlahla", "Nikhil", "Njabulo", "Nkosi", "Nqobani", "Nsizwa", "Owethu", "Phakama", "Phila", "Philani", "Qhude", "Qiniso", "Sakhile", "Sandile", "Sanele", "Sbu", "Sduduzo", "Senzo", "Sfiso", "Sibonele", "Sifiso", "Sihle", "Siphiwe", "Sipho", "Siyanda", "Siza", "Sizani", "Sizwe", "Tandie", "Thabo", "Thando", "Thatha", "Themba", "Thulani", "Ubaba", "Ukuza", "Ulwazi", "Umzuzu", "Unathi", "Uyise", "Vala", "Velaphi", "Vukani", "Vusi", "Vuyo", "Wandile", "Wenzile", "Xhegu", "Xhibeni", "Xolani", "Zamani", "Zenzele", "Zonke" };
    private static readonly string[] _femaleNames = new[] { "Adhama", "Adia", "Afiya", "Aishia", "Akina", "Aleela", "Aluna", "Amana", "Amanika", "Andaiye", "Angalia", "Arusi", "Ashura", "Asya", "Atiena", "Ayah", "Ayubu", "Bahati", "Bakari", "Baraka", "Barika", "Bavana", "Budhya", "Busara", "Chagina", "Chanua", "Chiku", "Chinira", "Dafina", "Dalia", "Dalila", "Dinka", "Elea", "Elewa", "Endana", "Endelea", "Fanaka", "Faraji", "Fikira", "Gethera", "Goma", "Halima", "Halina", "Halisi", "Hasana", "Hasina", "Hawa", "Heshima", "Himaya", "Hodari", "Imani", "Imara", "Inira", "Inithia", "Itanya", "Jaha", "Jahaira", "Jamaa", "Jamani", "Jamba", "Jehlani", "Jiona", "Julisha", "Kakena", "Kalere", "Kaluwa", "Kamara", "Kamaria", "Kanene", "Kanika", "Kanisa", "Karama", "Kenura", "Kesi", "Khadija", "Kiama", "Kiania", "Kibibi", "Kichea", "Kiira", "Kilinda", "Kinaya", "Kinjia", "Kito", "Koffi", "Kudio", "Kuende", "Kwashi", "Lindana", "Lindia", "Lisha", "Madini", "Mahiri", "Majda", "Maji", "Majida", "Malika", "Maliza", "Marini", "Mashika", "Masika", "Maulidi", "Milima", "Mkiwa", "Msia", "Mwamini", "Mwasaa", "Mwatabu", "Nadira", "Najuma", "Nbushe", "Neema", "Nigesa", "Nurisha", "Nyota", "Onyesha", "Otesha", "Oyana", "Panya", "Penda", "Radhiya", "Rasheda", "Rashida", "Rasida", "Raziya", "Rehema", "Risala", "Rukiya", "Saada", "Safika", "Safiri", "Salama", "Sanura", "Sauda", "Shani", "Shauri", "Siti", "Subira", "Taabu", "Tabara", "Taji", "Tamu", "Tia", "Tisa", "Tuliza", "Ujamaa", "Ujana", "Umija", "Usia", "Waseme", "Winda", "Zahra", "Zaida", "Zakiya", "Zawadi", "Zawati", "Zuri", "Zuwena", "Amahle", "Amehlo", "Andile", "Anele", "Anelisa", "Aphiwe", "Ayanda", "Ayize", "Bheka", "Bongani", "Bongile", "Bongiwe", "Buhle", "Cabanga", "Cebile", "Dade", "Dansa", "Davathi", "Deliwe", "Dida", "Didiza", "Dumile", "Ejaj", "Ekuseni", "Elethu", "Enama", "Enanela", "Esasa", "Ethuka", "Ethulo", "Ethwasa", "Fahlasi", "Fakazi", "Fikile", "Fudu", "Fula", "Funani", "Futhi", "Gagasi", "Gatsha", "Gede", "Gonothi", "Gqamile", "Gugu", "Icici", "Ifu", "Impela", "Impisi", "Indlovu", "Ingani", "Ingwe", "Inyoni", "Ishumi", "Izula", "Jabhile", "Jezile", "Kgabu", "Khanya", "Khaya", "Kholiwe", "Khule", "Khwezi", "Kwanele", "Lethiwe", "Lethu", "Lindiwe", "Londiwe", "Lungelo", "Lungile", "Luvo", "Lwandle", "Mbali", "Mhambi", "Minehle", "Mlilo", "Mondli", "Msizi", "Mthunzi", "Naledi", "Nandi", "Ndonsa", "Ndumiso", "Njabulo", "Njabulu", "Nobantu", "Nobuhle", "Nobuntu", "Nokwazi", "Nolwazi", "Nomcebo", "Nompilo", "Nomsa", "Nomusa", "Nomvula", "Nomzamo", "Nonhle", "Nosipho", "Noxolo", "Nozipho", "Nozizwe", "Nqobile", "Ntokozo", "Ntombi", "Ogogo", "Philile", "Qhikiza", "Qiniso", "Ramla", "Sakhile", "Sandile", "Sibahle", "Sifiso", "Sihle", "Siphiwe", "Sipho", "Siyanda", "Siza", "Sizani", "Sizwe", "Sonto", "Thabile", "Thabisa", "Thandie", "Thando", "Themba", "Thembi", "Thoko", "Thulani", "Thulile", "Uju", "Ukudala", "Ukuza", "Umakoti", "Umzuzu", "Unathi", "Unina", "Usizo", "Vala", "Velile", "Vemvane", "Vumela", "Vumile", "Vuyiswa", "Welile", "Winile", "Xolani", "Xolile", "Zamani", "Zamile", "Zandile", "Zanele", "Zibu", "Zinhle", "Zodwa", "Zola", "Zongile", "Zonke", "Zula" };

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _isSolved = false;
        _selectedLion = -1;
        _startAngle = Rnd.Range(0f, 360f);
        _colorblindEnabled = ColorblindMode.ColorblindModeActive;

        // Generate rules
        var rnd = RuleSeedable.GetRNG();
        Lion[] pride;
        KeyValuePair<string, bool>[] visitingLionNames;
        int leadHuntressColorIx, entitlementKing, entitlementAdultSibling, entitlementAdult, entitlementCubSibling, entitlementCub, entitlementIndKing, entitlementIndSibling, entitlementIndMale, entitlementIndFemale, entitlementSn;
        IndicatorRule indicatorRule;
        if (rnd.Seed == 1)
        {
            // Default rules
            pride = _allLions;
            visitingLionNames = _visitingLionNames;
            entitlementKing = 10;
            entitlementAdultSibling = 7;
            entitlementAdult = 5;
            entitlementCubSibling = 4;
            entitlementCub = 3;
            entitlementIndKing = 4;
            entitlementIndSibling = 3;
            entitlementIndMale = 2;
            entitlementIndFemale = 1;
            entitlementSn = 1;
            leadHuntressColorIx = 0;
            indicatorRule = IndicatorRule.Lit;
        }
        else
        {
            while (true)
            {
                // Pick 35 lion names
                var allNames = rnd.ShuffleFisherYates(_maleNames.Concat(_femaleNames).ToArray());
                var candidate = 0;
                var lionNames = new List<string>();
                while (lionNames.Count < 35)
                {
                    var name = allNames[candidate++];
                    if (!lionNames.Contains(name))
                        lionNames.Add(name);
                }

                // Decide which lions are in the pride and which ones are visiting
                pride = lionNames.Take(18).Select(name => new Lion { Name = name, Male = !_maleNames.Contains(name) ? false : !_femaleNames.Contains(name) ? true : rnd.Next(0, 2) == 0 }).ToArray();
                visitingLionNames = lionNames.Skip(18).Select(name => new KeyValuePair<string, bool>(name, !_maleNames.Contains(name) ? false : !_femaleNames.Contains(name) ? true : rnd.Next(0, 2) == 0)).ToArray();
                var birthyear = new int[18];

                // Decide on a random birthyear and lifespan for each lion (lifespan includes “unborn” phase)
                for (var i = 0; i < pride.Length; i++)
                {
                    var lifespan = rnd.Next(2, 15);
                    var cubhood = new[] { 1, 2, 2, 2, 2, 2, 3 }[rnd.Next(0, 6)];
                    birthyear[i] = rnd.Next(Math.Max(-5, 2 - lifespan), 17);
                    pride[i].Status = new LionStatus[16];
                    for (var yr = 1; yr <= 16; yr++)
                        pride[i].Status[yr - 1] =
                            yr < birthyear[i] ? LionStatus.Null :
                            yr == birthyear[i] ? LionStatus.Unborn :
                            yr < birthyear[i] + cubhood + 1 ? LionStatus.Cub :
                            yr < birthyear[i] + lifespan ? (rnd.Next(0, 8) == 0 ? LionStatus.Absent : LionStatus.Adult) :
                            LionStatus.Dead;
                }

                // Make sure there’s an adult male and an adult female in every year so we can always have a king and a lead huntress
                var valid = true;
                int? currentKing = null;
                for (var yr = 0; (yr < 16) && valid; yr++)
                {
                    var potentialKings = pride.SelectIndexWhere(lion => lion.Male && lion.Status[yr] == LionStatus.Adult).ToArray();
                    var potentialMothers = pride.SelectIndexWhere(lion => !lion.Male && lion.Status[yr] == LionStatus.Adult).ToArray();
                    if (potentialKings.Length == 0 || potentialMothers.Length == 0)
                        valid = false;
                    else
                    {
                        // Assign a king
                        if (currentKing == null || !potentialKings.Contains(currentKing.Value))
                            currentKing = potentialKings[rnd.Next(0, potentialKings.Length)];
                        pride[currentKing.Value].Status[yr] = LionStatus.King;

                        // Give greater weight to younger mothers
                        Array.Sort(potentialMothers, (a, b) =>
                        {
                            var val = birthyear[b] - birthyear[a];
                            return val != 0 ? val : pride[a].Name.CompareTo(pride[b].Name);
                        });
                        var chooseFrom = new List<int>();
                        for (var i = 0; i < potentialMothers.Length; i++)
                            for (var j = 0; j <= i; j++)
                                chooseFrom.Add(potentialMothers[j]);

                        // Give every unborn cub a mother
                        for (var i = 0; i < pride.Length; i++)
                            if (pride[i].Status[yr] == LionStatus.Unborn)
                                pride[i].Mother = pride[chooseFrom[rnd.Next(0, chooseFrom.Count)]].Name;
                    }
                }
                if (!valid)
                    continue;

                // Sort by birthyear
                var indexes = Enumerable.Range(0, pride.Length).ToArray();
                Array.Sort(indexes, (a, b) =>
                {
                    var val = birthyear[a] - birthyear[b];
                    return val != 0 ? val : pride[a].Name.CompareTo(pride[b].Name);
                });
                pride = indexes.Select(ix => pride[ix]).ToArray();

                // Make sure that the lifespans won’t overlap on the chart
                valid = true;
                for (var i = 0; (i < 6) && valid; i++)
                    if (pride[i + 12].Status[Array.IndexOf(pride[i].Status, LionStatus.Dead)] != LionStatus.Null)
                        valid = false;
                if (!valid)
                    continue;

                break;
            }

            entitlementKing = rnd.Next(10, 20);
            entitlementAdultSibling = entitlementKing - rnd.Next(1, 4);
            entitlementAdult = entitlementAdultSibling - rnd.Next(1, 3);
            entitlementCubSibling = entitlementAdult - rnd.Next(1, 4);
            entitlementCub = Math.Max(1, entitlementCubSibling - rnd.Next(1, 4));

            indicatorRule = (IndicatorRule) rnd.Next(0, 3);

            entitlementIndKing = rnd.Next(3, 6);
            entitlementIndSibling = entitlementIndKing - rnd.Next(1, 3);
            entitlementIndMale = rnd.Next(1, 3);
            entitlementIndFemale = 3 - entitlementIndMale;
            entitlementSn = rnd.Next(1, 3);

            leadHuntressColorIx = rnd.Next(0, 8);

            Debug.LogFormat("<Lion’s Share #{0}> Pride:\n{1}", _moduleId, pride.Select(l => string.Format("{0} ({1}) = {2} / {3}", l.Name, l.Male ? "m" : "f", l.Status.Select(val => (int) val).JoinString(), l.Mother)).JoinString("\n"));
            Debug.LogFormat(@"<Lion’s Share #{0}> Numbers: {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, LHC={11}", _moduleId,
                entitlementKing, entitlementAdultSibling, entitlementAdult, entitlementCubSibling, entitlementCub,
                entitlementIndKing, entitlementIndSibling, entitlementIndMale, entitlementIndFemale, entitlementSn, leadHuntressColorIx);
        }


        // Generate puzzle
        var year = Rnd.Range(0, 16);
        Year.text = (year + 1).ToString();

        retry:
        var lions = pride.Where(l => l.Status[year] != LionStatus.Null).ToList().Shuffle();
        while (lions.Count > 8)
            lions.RemoveAt(Rnd.Range(0, lions.Count));
        var numRemove = new[] { 0, 1, 1, 1, 2, 3 }[Rnd.Range(0, 5)];
        for (int i = 0; i < numRemove; i++)
            lions.RemoveAt(Rnd.Range(0, lions.Count));
        var visitingLions = visitingLionNames.ToList();
        for (int i = Rnd.Range(0, lions.Count / 2); i > 0; i--)
        {
            var visitor = visitingLions[Rnd.Range(0, visitingLions.Count)];
            lions[i] = new Lion { Name = visitor.Key, Male = visitor.Value, Status = new LionStatus[16] };
            lions[i].Status[year] = LionStatus.Visiting;
            visitingLions.Remove(visitor);
        }

        // We need a lead huntress
        var leadHuntress = lions.FirstOrDefault(l => !l.Male && l.Status[year] == LionStatus.Adult);
        if (leadHuntress == null)
            goto retry;

        // Make sure that every unborn cub has their mother present
        if (lions.Any(cub => cub.Status[year] == LionStatus.Unborn && !lions.Any(mother => mother.Name == cub.Mother)))
            goto retry;

        // Make sure we have more than 1 lion to apportion prey to
        if (lions.Count(l => l.Status[year] != LionStatus.Unborn && l.Status[year] != LionStatus.Dead && l.Status[year] != LionStatus.Absent) < 2)
            goto retry;

        // Shuffle up the colors, but make sure the color for the lead huntress is in range
        var hues = new List<int> { 0, 28, 61, 123, 180, 232, 270, 303 };
        var leadHuntressHue = hues[leadHuntressColorIx];
        do
            hues.Shuffle();
        while (hues.IndexOf(leadHuntressHue) >= lions.Count);

        // Make sure the lead huntress will have the correct color
        lions.Remove(leadHuntress);
        lions.Insert(hues.IndexOf(leadHuntressHue), leadHuntress);

        _lionNames = lions.Select(l => l.Name).ToArray();

        Debug.LogFormat(@"[Lion’s Share #{0}] Year: {1}", _moduleId, year + 1);
        Debug.LogFormat(@"[Lion’s Share #{0}] Lions present: {1}", _moduleId, lions
            .OrderBy(l => l.Name)
            .Select(l => string.Format("{0} ({1} {2})", l.Name, l.Male ? "male" : "female", l == leadHuntress ? "adult; lead huntress" : l.Status[year].ToString().ToLowerInvariant()))
            .JoinString(", "));

        _selectedPieSliceColors = hues.Select(hue => Color.HSVToRGB(hue / 360f, .7f, 1f)).ToArray();
        _unselectedPieSliceColors = hues.Select(hue => Color.HSVToRGB(hue / 360f, .6f, .7f)).ToArray();
        _leadHuntressIx = lions.IndexOf(leadHuntress);

        var entitlement = new int[lions.Count];
        var kingsMother = pride.Where(l => l.Status[year] == LionStatus.King).Select(l => l.Mother).FirstOrDefault();
        var table = new int[lions.Count][];

        for (int i = 0; i < lions.Count; i++)
        {
            table[i] = new int[5];

            // The King, if present, has 10 units of entitlement.
            if (lions[i].Status[year] == LionStatus.King)
                entitlement[i] = entitlementKing;
            // Any adult siblings of the King have 7 units each.
            // All other adults have 5 units each.
            else if (lions[i].Status[year] == LionStatus.Adult)
                entitlement[i] = lions[i].Mother != "" && lions[i].Mother == kingsMother ? entitlementAdultSibling : entitlementAdult;
            // Any cub siblings of the King have 4 units each.
            // All other cubs have 3 units.
            else if (lions[i].Status[year] == LionStatus.Cub)
                entitlement[i] = lions[i].Mother != "" && lions[i].Mother == kingsMother ? entitlementCubSibling : entitlementCub;
            // Lions who do not belong to the pride have only 1 unit.
            else if (lions[i].Status[year] == LionStatus.Visiting)
                entitlement[i] = 1;

            table[i][0] = entitlement[i];
        }

        for (int i = 0; i < lions.Count; i++)
        {
            var lionName = lions[i].Name.ToUpperInvariant();

            // For each lit indicator on the bomb that contains a lion’s name’s first letter, add 4 units for the King, 3 for their adult siblings*, 2 units for any other males and 1 for females.
            var indicatorBonus = Bomb.GetOnIndicators().Count(ind => ind.Contains(lionName[0])) * (
                lions[i].Status[year] == LionStatus.King ? entitlementIndKing :
                lions[i].Status[year] == LionStatus.Adult && lions[i].Mother != "" && lions[i].Mother == kingsMother ? entitlementIndSibling :
                lions[i].Male ? entitlementIndMale : entitlementIndFemale);
            entitlement[i] += indicatorBonus;
            table[i][1] = indicatorBonus;

            // For each serial number letter contained in a lion’s name, add 1 unit.
            var serialNumberBonus = entitlementSn * Bomb.GetSerialNumberLetters().Count(letter => lionName.Contains(letter));
            entitlement[i] += serialNumberBonus;
            table[i][2] = serialNumberBonus;
        }

        // Each unborn cub adds 1 unit to their mother’s entitlement.
        // Lions who are dead or absent have no entitlement.
        for (int i = 0; i < lions.Count; i++)
        {
            if (lions[i].Status[year] == LionStatus.Unborn)
            {
                var motherIx = lions.IndexOf(lion => lion != null && lion.Name == lions[i].Mother);
                table[motherIx][3]++;
                entitlement[motherIx]++;
            }

            if (lions[i].Status[year] == LionStatus.Unborn || lions[i].Status[year] == LionStatus.Absent || lions[i].Status[year] == LionStatus.Dead)
            {
                lions[i] = null;
                entitlement[i] = 0;
            }
        }

        var totalEntitlement = entitlement.Sum();
        Debug.LogFormat(@"[Lion’s Share #{0}] Total entitlement: {1}", _moduleId, totalEntitlement);
        Debug.LogFormat(@"[Lion’s Share #{0}] Apportion prey to {1} lions:", _moduleId, lions.Count(l => l != null));

        var textTable = new StringBuilder();
        textTable.AppendLine(@"        │   Base│Indictr│Serial#│ Unborn│       │       │   Lead│  Final");
        textTable.AppendLine(@"Lion    │entlmnt│  bonus│  bonus│   cubs│Entlmnt│Portion│huntrss│portion");
        textTable.AppendLine(@"────────┼───────┼───────┼───────┼───────┼───────┼───────┼───────┼───────");
        _correctPortions = new int[lions.Count];
        for (int i = 0; i < lions.Count; i++)
            _correctPortions[i] = entitlement[i] * 100 / totalEntitlement;
        Func<int, string> sgn = num => num == 0 ? "" : "+" + num;
        var logLines = new[] { new { Name = "", Line = "" } }.ToList();
        logLines.Clear();
        for (int i = 0; i < lions.Count; i++)
        {
            if (lions[i] == null)
                continue;
            var prevPortion = _correctPortions[i];
            var leadHuntressBonus = lions[i] == leadHuntress ? 100 - _correctPortions.Sum() : 0;
            _correctPortions[i] += leadHuntressBonus;
            logLines.Add(new
            {
                lions[i].Name,
                Line = string.Format(@"{0,-8}│{1,7}│{2,7}│{3,7}│{4,7}│{5,7}│{6,7}│{7,7}│{8,7}",
                    lions[i].Name, table[i][0], sgn(table[i][1]), sgn(table[i][2]), sgn(table[i][3]), entitlement[i], prevPortion + "%", sgn(leadHuntressBonus), _correctPortions[i] + "%")
            });
        }
        foreach (var line in logLines.OrderBy(l => l.Name))
            textTable.AppendLine(line.Line);
        Debug.Log(textTable.ToString());

        _currentPortions = Enumerable.Range(0, _lionNames.Length).Select(l => 10).ToArray();
        while (_currentPortions.Sum() < 100)
            _currentPortions[Rnd.Range(0, _currentPortions.Length)] += Rnd.Range(0, 100 - _currentPortions.Sum()) + 1;

        for (int i = 0; i < _lionNames.Length; i++)
            LionSelectables[i].OnInteract = lionClick(i);
        for (int i = _lionNames.Length; i < PieSlices.Length; i++)
        {
            PieSlices[i].gameObject.SetActive(false);
            LionSelectables[i].gameObject.SetActive(false);
        }
        ButtonInc.OnInteract = incClick;
        ButtonDec.OnInteract = decClick;
        ButtonSubmit.OnInteract = submitClick;

        updatePie();
    }

    private bool submitClick()
    {
        if (_isSolved)
            return false;

        if (_currentPortions.SequenceEqual(_correctPortions))
        {
            Debug.LogFormat(@"[Lion’s Share #{0}] Module solved.", _moduleId);
            Module.HandlePass();
            Year.text = "";
            _isSolved = true;
            _selectedLion = -1;
            updatePie();
            Audio.PlaySoundAtTransform("Roar" + Rnd.Range(1, 7), transform);
        }
        else
        {
            Debug.LogFormat(@"[Lion’s Share #{0}] Incorrect solution submitted ({1}).", _moduleId, _lionNames.Select((l, ix) => string.Format(@"{0}={1}%", l, _currentPortions[ix])).JoinString(", "));
            Module.HandleStrike();
        }
        return false;
    }

    private bool decClick()
    {
        if (_selectedLion == -1 || _isSolved)
            return false;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonDec.transform);
        ButtonDec.AddInteractionPunch(.5f);
        var otherLion = Enumerable.Range(0, _lionNames.Length).FirstOrDefault(ix => ix != _selectedLion && _currentPortions[ix] != 0);
        if (otherLion == _selectedLion || _currentPortions[otherLion] == 0)
            return false;
        if (_currentPortions[_selectedLion] == 1 && _correctPortions[_selectedLion] != 0)
        {
            Debug.LogFormat(@"[Lion’s Share #{0}] Setting {1} to zero would be incorrect. Strike.", _moduleId, _lionNames[_selectedLion]);
            Module.HandleStrike();
        }
        else
        {
            _currentPortions[otherLion]++;
            _currentPortions[_selectedLion]--;
            _startAngle += otherLion > _selectedLion ? 1.8f : -1.8f;
            if (_currentPortions[_selectedLion] == 0)
                _selectedLion = -1;
            updatePie();
        }
        return false;
    }

    private bool incClick()
    {
        if (_selectedLion == -1 || _isSolved)
            return false;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonInc.transform);
        ButtonInc.AddInteractionPunch(.5f);
        var otherLion = Enumerable.Range(0, _lionNames.Length).FirstOrDefault(ix => ix != _selectedLion && _currentPortions[ix] > 1);
        if (otherLion == _selectedLion || _currentPortions[otherLion] <= 1)
            return false;
        _currentPortions[otherLion]--;
        _currentPortions[_selectedLion]++;
        _startAngle += otherLion > _selectedLion ? -1.8f : 1.8f;
        updatePie();
        return false;
    }

    private KMSelectable.OnInteractHandler lionClick(int i)
    {
        return delegate
        {
            if (_isSolved)
                return false;
            _selectedLion = i;
            updatePie();
            return false;
        };
    }

    private void updatePie()
    {
        float runningAngle = _startAngle;
        for (int i = 0; i < _lionNames.Length; i++)
        {
            if (_currentPortions[i] == 0)
            {
                PieSlices[i].gameObject.SetActive(false);
                LionSelectables[i].gameObject.SetActive(false);
            }
            else
            {
                PieSlices[i].transform.localPosition = new Vector3(0, i == _selectedLion ? .09f : .03f, 0);
                PieSlices[i].transform.localEulerAngles = new Vector3(0, runningAngle, 0);
                PieSlices[i].material.color = (i == _selectedLion ? _selectedPieSliceColors : _unselectedPieSliceColors)[i];
                PieMeshFilters[i].mesh = PieSliceMeshes[_currentPortions[i] - 1];
                LionNameMeshes[i].text = string.Format("<size=65>{1}%</size> {0}", _colorblindEnabled && i == _leadHuntressIx ? _lionNames[i].ToUpperInvariant() : _lionNames[i], _currentPortions[i]);
                LionSelectables[i].transform.localPosition = new Vector3(0, i == _selectedLion ? .091f : .031f, 0);
                LionSelectables[i].transform.localEulerAngles = new Vector3(0, runningAngle + .5f * _currentPortions[i] * 3.6f, 0);
                runningAngle += _currentPortions[i] * 3.6f;
                PieSlices[i].gameObject.SetActive(true);
                LionSelectables[i].gameObject.SetActive(true);
            }
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} set Simba 12 [set the percentage for a lion] | !{0} submit [chain with commas]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (_isSolved)
        {
            yield return "sendtochaterror Module already solved. Look harder!";
            yield break;
        }

        if (Regex.IsMatch(command, @"^\s*color\s*blind\s*$", RegexOptions.IgnoreCase))
        {
            _colorblindEnabled = true;
            updatePie();
            yield return null;
            yield break;
        }

        var stuffToDo = new List<object>();
        foreach (var subcommand in command.Split(','))
        {
            Match m;
            if ((m = Regex.Match(subcommand, @"^\s*(?:set\s+)?(\w+)\s+(\d+)\s*$", RegexOptions.IgnoreCase)).Success)
            {
                var lionIx = _lionNames.IndexOf(l => l.Equals(m.Groups[1].Value, StringComparison.InvariantCultureIgnoreCase));
                if (lionIx == -1 || _currentPortions[lionIx] == 0)
                {
                    yield return string.Format("sendtochaterror “{0}”? Who ya callin’ oopid-stay?", m.Groups[1].Value);
                    yield break;
                }
                int value;
                if (!int.TryParse(m.Groups[2].Value, out value) || value > 100)
                {
                    yield return string.Format("sendtochaterror “{0}%”? Didn’t your mother ever tell you not to play with your food?", m.Groups[2].Value);
                    yield break;
                }
                stuffToDo.Add(new[] { LionSelectables[lionIx] });
                stuffToDo.Add(new Func<object>(() => Enumerable.Repeat(value > _currentPortions[lionIx] ? ButtonInc : ButtonDec, Math.Abs(value - _currentPortions[lionIx])).ToArray()));
            }
            else if ((m = Regex.Match(subcommand, @"^\s*submit\s*$", RegexOptions.IgnoreCase)).Success)
            {
                stuffToDo.Add(new[] { ButtonSubmit });
            }
            else
            {
                yield return string.Format("sendtochaterror “{0}”? Asante sana, squash banana! Wewe nugu, mimi apana!", subcommand.Trim());
                yield break;
            }
        }

        if (stuffToDo.Count > 0)
        {
            yield return null;
            foreach (var stuff in stuffToDo)
                yield return stuff is Func<object> ? ((Func<object>) stuff)() : stuff;
        }
    }
}
