using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public Mesh[] PieSliceMeshes;
    public MeshRenderer[] PieSlices;
    public KMSelectable[] LionSelectables;
    private MeshFilter[] _pieMeshFilters;
    private TextMesh[] _lionNameMeshes;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

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

    sealed class Lion
    {
        public string Name;
        public string Mother;
        public bool Male;
        public LionStatus[] Status;
    }

    private static readonly KeyValuePair<string, bool>[] _visitingLionNames = "Tojo,m;Chumvi,m;Malka,m;Askari,m;Tama,f;Rani,f;Zuri,f;Tiifu,f;Kula,f;Naanda,f;Ndona,f;Sheena,f;Diku,f;Boga,f;Sabini,f;Babu,f;Weena"
        .Split(',').Select(str => str.Split(','))
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
Mheetu,m,Sarabi,000001223344433
Zira,f,Sarafina,0000012233333
Nala,f,Sarafina,0000122333433333
Simba,m,Sarabi,000124444445555
Sarabi,f,,22333333333333
Sarafina,f,,223333333333"
        .Replace("\r", "").Split('\n').Select(str => str.Split(','))
        .Select(arr => new Lion { Name = arr[0], Male = arr[1] == "m", Mother = arr[2], Status = arr[3].PadRight(16, '6').Select(ch => (LionStatus) (ch - '0')).ToArray() }).ToArray();

    private string[] _lionNames;
    private int[] _currentPortions;
    private int[] _correctPortions;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _pieMeshFilters = PieSlices.Select(obj => obj.GetComponent<MeshFilter>()).ToArray();
        _lionNameMeshes = LionSelectables.Select(obj => obj.transform.Find("Lion name").GetComponent<TextMesh>()).ToArray();
        Array.Sort(PieSliceMeshes.Select(m => int.Parse(m.name.Substring(5))).ToArray(), PieSliceMeshes);

        var year = Rnd.Range(0, 16);

        retry:
        var lions = _allLions.Where(l => l.Status[year] != LionStatus.Null).ToList().Shuffle();
        while (lions.Count > 8)
            lions.RemoveAt(Rnd.Range(0, lions.Count));
        var numRemove = new[] { 0, 1, 1, 1, 2, 3 }[Rnd.Range(0, 5)];
        for (int i = 0; i < numRemove; i++)
            lions.RemoveAt(Rnd.Range(0, lions.Count));
        var visitingLions = _visitingLionNames.ToList();
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

        // Make sure the lead huntress is red
        lions.Remove(leadHuntress);
        lions.Insert(0, leadHuntress);

        _lionNames = lions.Select(l => l.Name).ToArray();

        Debug.LogFormat(@"[Lion’s Share #{0}] Lions present: {1}", _moduleId, lions
            .OrderBy(l => l.Name)
            .Select(l => string.Format("{0} ({1} {2})", l.Name, l.Male ? "male" : "female", l == leadHuntress ? "adult; lead huntress" : l.Status[year].ToString().ToLowerInvariant()))
            .JoinString(", "));

        var w = 1f / lions.Count;
        var pieSliceColors = Enumerable.Range(0, lions.Count).Select(i => Color.HSVToRGB(w * i, .7f, .8f)).ToList();
        // Make sure to keep red at the front for the lead huntress
        var red = pieSliceColors[0];
        pieSliceColors.RemoveAt(0);
        pieSliceColors.Shuffle();
        pieSliceColors.Insert(0, red);

        var entitlement = new int[lions.Count];
        var kingsMother = lions.Where(l => l.Status[year] == LionStatus.King).Select(l => l.Mother).FirstOrDefault();
        var table = new int[lions.Count][];

        for (int i = 0; i < lions.Count; i++)
        {
            table[i] = new int[5];

            // The King, if present, has 10 units of entitlement.
            if (lions[i].Status[year] == LionStatus.King)
                entitlement[i] = 10;
            // Any adult siblings* of the King have 7 units each.
            // All other adults have 5 units each.
            else if (lions[i].Status[year] == LionStatus.Adult)
                entitlement[i] = lions[i].Mother == kingsMother ? 7 : 5;
            // Any cub siblings of the King have 4 units each.
            // All other cubs have 3 units.
            else if (lions[i].Status[year] == LionStatus.Cub)
                entitlement[i] = lions[i].Mother == kingsMother ? 4 : 3;
            // Lions who do not belong to the pride have only 1 unit.
            else if (lions[i].Status[year] == LionStatus.Visiting)
                entitlement[i] = 1;

            table[i][0] = entitlement[i];
        }

        for (int i = 0; i < lions.Count; i++)
        {
            var lionName = lions[i].Name.ToUpperInvariant();

            // For each lit indicator on the bomb that contains a lion’s name’s first letter, add 4 units for the King, 3 for their adult siblings*, 2 units for any other males and 1 for females.
            var indicatorBonus = Bomb.GetOnIndicators().Count(ind => ind.Any(letter => lionName.Contains(letter))) * (
                lions[i].Status[year] == LionStatus.King ? 4 :
                lions[i].Status[year] == LionStatus.Adult && lions[i].Mother == kingsMother ? 3 :
                lions[i].Male ? 2 : 1);
            entitlement[i] += indicatorBonus;
            table[i][1] = indicatorBonus;

            // For each serial number letter contained in a lion’s name, add 1 unit.
            var serialNumberBonus = Bomb.GetSerialNumberLetters().Count(letter => lionName.Contains(letter));
            entitlement[i] += serialNumberBonus;
            table[i][2] = serialNumberBonus;
        }

        // Each unborn cub adds 1 unit to their mother’s entitlement.
        // Lions who are dead or absent have no entitlement.
        for (int i = 0; i < lions.Count; i++)
        {
            if (lions[i].Status[year] == LionStatus.Unborn)
            {
                table[lions.IndexOf(lion => lion != null && lion.Name == lions[i].Mother)][3]++;
                lions[i] = null;
            }
            else if (lions[i].Status[year] == LionStatus.Absent || lions[i].Status[year] == LionStatus.Dead)
                lions[i] = null;
        }

        Debug.LogFormat(@"[Lion’s Share #{0}] Apportion prey to {1} lions:", _moduleId, lions.Count(l => l != null));

        var textTable = new StringBuilder();
        textTable.AppendLine(@"        │   Base│Indictr│Serial#│ Unborn│       │       │   Lead│  Final");
        textTable.AppendLine(@"Lion    │entlmnt│  bonus│  bonus│   cubs│Entlmnt│Portion│huntrss│Portion");
        textTable.AppendLine(@"────────┼───────┼───────┼───────┼───────┼───────┼───────┼───────┼───────");
        var totalEntitlement = entitlement.Sum();
        _correctPortions = new int[lions.Count];
        for (int i = 0; i < lions.Count; i++)
            _correctPortions[i] = entitlement[i] * 100 / totalEntitlement;
        Func<int, string> sgn = num => num == 0 ? "" : "+" + num;
        for (int i = 0; i < lions.Count; i++)
        {
            if (lions[i] == null)
                continue;
            var prevPortion = _correctPortions[i];
            var leadHuntressBonus = lions[i] == leadHuntress ? 100 - _correctPortions.Sum() : 0;
            _correctPortions[i] += leadHuntressBonus;
            textTable.AppendFormat(@"{0,-8}│{1,7}│{2,7}│{3,7}│{4,7}│{5,7}│{6,7}│{7,7}│{8,7}{9}",
                lions[i].Name, table[i][0], sgn(table[i][1]), sgn(table[i][2]), sgn(table[i][3]), entitlement[i], prevPortion, sgn(leadHuntressBonus), _correctPortions[i], Environment.NewLine);
        }
        Debug.Log(textTable.ToString());

        _currentPortions = Enumerable.Range(0, _lionNames.Length).Select(l => 10).ToArray();
        while (_currentPortions.Sum() < 100)
            _currentPortions[Rnd.Range(0, _currentPortions.Length)] += Rnd.Range(0, 100 - _currentPortions.Sum()) + 1;

        float runningAngle = 0;
        for (int i = 0; i < _lionNames.Length; i++)
        {
            PieSlices[i].material.color = pieSliceColors[i];
            PieSlices[i].transform.localEulerAngles = new Vector3(0, runningAngle, 0);
            _pieMeshFilters[i].mesh = PieSliceMeshes[_currentPortions[i] - 1];
            _lionNameMeshes[i].text = _lionNames[i];
            LionSelectables[i].transform.localEulerAngles = new Vector3(0, runningAngle + .5f * _currentPortions[i] * 3.6f, 0);
            runningAngle += _currentPortions[i] * 3.6f;
        }

        for (int i = _lionNames.Length; i < PieSlices.Length; i++)
        {
            PieSlices[i].gameObject.SetActive(false);
            LionSelectables[i].gameObject.SetActive(false);
        }
    }
}
