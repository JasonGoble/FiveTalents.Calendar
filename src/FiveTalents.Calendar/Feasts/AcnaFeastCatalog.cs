using FiveTalents.Calendar.Calendar;

namespace FiveTalents.Calendar.Feasts;

/// <summary>
/// Fixed and moveable feast days, and optional commemorations, per the ACNA
/// Book of Common Prayer 2019 calendar.
/// </summary>
internal static class AcnaFeastCatalog
{
    private readonly record struct MonthDay(int Month, int Day);

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all Principal Feasts and Holy Days (rank Major or above) that fall
    /// on the given date. Multiple feasts may share a date; caller resolves precedence.
    /// </summary>
    public static IReadOnlyList<FeastDay> GetHolyDays(DateOnly date, int year)
    {
        var easter = EasterCalculator.GetEaster(year);
        List<FeastDay> feasts = new List<FeastDay>();

        if (_fixedHolyDays.TryGetValue(new MonthDay(date.Month, date.Day), out var fixed_))
        {
            feasts.Add(fixed_);
        }

        foreach (var (feast, offset) in _moveableHolyDays)
        {
            if (easter.AddDays(offset) == date)
            {
                feasts.Add(feast);
            }
        }

        return feasts;
    }

    /// <summary>
    /// Returns all optional commemorations (Anglican and Ecumenical, rank below Major)
    /// observed on the given date.
    /// </summary>
    public static IReadOnlyList<FeastDay> GetCommemorations(DateOnly date, int year)
    {
        var easter = EasterCalculator.GetEaster(year);
        List<FeastDay> result = new List<FeastDay>();

        if (_fixedCommemorations.TryGetValue(new MonthDay(date.Month, date.Day), out var list))
        {
            result.AddRange(list);
        }

        // Rogation Days: Mon, Tue, Wed before Ascension (Easter + 36, 37, 38)
        for (int offset = 36; offset <= 38; offset++)
        {
            if (easter.AddDays(offset) == date)
            {
                result.Add(new FeastDay { Name = "Rogation Day", Rank = FeastRank.Minor });
            }
        }

        return result;
    }

    /// <summary>
    /// Returns true if the given date is one of the twelve Ember Days per year.
    /// Ember Days fall on the Wed, Fri, and Sat after: the First Sunday of Lent,
    /// Pentecost, Holy Cross Day (Sep 14), and St. Lucy's Day (Dec 13).
    /// </summary>
    public static bool IsEmberDay(DateOnly date, int year)
    {
        var easter = EasterCalculator.GetEaster(year);

        // Moveable anchors
        var firstSundayOfLent = easter.AddDays(-42); // Ash Wed = Easter-46, first Lent Sunday = Easter-42
        var pentecost = easter.AddDays(49);

        foreach (var anchor in new[]
        {
            firstSundayOfLent,
            pentecost,
            new DateOnly(year, 9, 14),   // Holy Cross Day
            new DateOnly(year, 12, 13),  // St. Lucy's Day
        })
        {
            if (IsEmberDayAfterAnchor(date, anchor))
            {
                return true;
            }
        }

        return false;
    }

    // ── Moveable Holy Days (offset in days from Easter Sunday) ───────────────

    private static readonly (FeastDay feast, int offset)[] _moveableHolyDays =
    [
        (new FeastDay { Name = "Ash Wednesday",              Rank = FeastRank.Principal, Color = LiturgicalColor.Purple, IsMoveable = true }, -46),
        (new FeastDay { Name = "Palm Sunday",                Rank = FeastRank.Principal, Color = LiturgicalColor.Red,    IsMoveable = true }, -7),
        (new FeastDay { Name = "Monday of Holy Week",        Rank = FeastRank.Major,     Color = LiturgicalColor.Purple, IsMoveable = true }, -6),
        (new FeastDay { Name = "Tuesday of Holy Week",       Rank = FeastRank.Major,     Color = LiturgicalColor.Purple, IsMoveable = true }, -5),
        (new FeastDay { Name = "Wednesday of Holy Week",     Rank = FeastRank.Major,     Color = LiturgicalColor.Purple, IsMoveable = true }, -4),
        (new FeastDay { Name = "Maundy Thursday",            Rank = FeastRank.Principal, Color = LiturgicalColor.White,  IsMoveable = true }, -3),
        (new FeastDay { Name = "Good Friday",                Rank = FeastRank.Principal, Color = LiturgicalColor.Black,  IsMoveable = true }, -2),
        (new FeastDay { Name = "Holy Saturday",              Rank = FeastRank.Principal, Color = LiturgicalColor.Black,  IsMoveable = true }, -1),
        (new FeastDay { Name = "Easter Day",                 Rank = FeastRank.Principal, Color = LiturgicalColor.White,  IsMoveable = true },  0),
        (new FeastDay { Name = "Monday in Easter Week",      Rank = FeastRank.Major,     Color = LiturgicalColor.White,  IsMoveable = true },  1),
        (new FeastDay { Name = "Tuesday in Easter Week",     Rank = FeastRank.Major,     Color = LiturgicalColor.White,  IsMoveable = true },  2),
        (new FeastDay { Name = "Wednesday in Easter Week",   Rank = FeastRank.Major,     Color = LiturgicalColor.White,  IsMoveable = true },  3),
        (new FeastDay { Name = "Thursday in Easter Week",    Rank = FeastRank.Major,     Color = LiturgicalColor.White,  IsMoveable = true },  4),
        (new FeastDay { Name = "Friday in Easter Week",      Rank = FeastRank.Major,     Color = LiturgicalColor.White,  IsMoveable = true },  5),
        (new FeastDay { Name = "Saturday in Easter Week",    Rank = FeastRank.Major,     Color = LiturgicalColor.White,  IsMoveable = true },  6),
        (new FeastDay { Name = "Ascension Day",              Rank = FeastRank.Principal, Color = LiturgicalColor.White,  IsMoveable = true }, 39),
        (new FeastDay { Name = "The Day of Pentecost",       Rank = FeastRank.Principal, Color = LiturgicalColor.Red,    IsMoveable = true }, 49),
        (new FeastDay { Name = "Trinity Sunday",             Rank = FeastRank.Principal, Color = LiturgicalColor.White,  IsMoveable = true }, 56),
    ];

    // ── Fixed Holy Days (rank Major or above) ─────────────────────────────────

    private static readonly Dictionary<MonthDay, FeastDay> _fixedHolyDays = new()
    {
        // January
        [new(1, 1)] = new() { Name = "The Circumcision and Holy Name of Our Lord Jesus Christ", Rank = FeastRank.Major, Color = LiturgicalColor.White },
        [new(1, 6)] = new() { Name = "The Epiphany of Our Lord Jesus Christ", Rank = FeastRank.Principal, Color = LiturgicalColor.White },
        [new(1, 18)] = new() { Name = "Confession of Peter the Apostle", Rank = FeastRank.Major, Color = LiturgicalColor.White },
        [new(1, 25)] = new() { Name = "Conversion of Paul the Apostle", Rank = FeastRank.Major, Color = LiturgicalColor.White },

        // February
        [new(2, 2)] = new() { Name = "The Presentation of Our Lord Jesus Christ in the Temple", Rank = FeastRank.Major, Color = LiturgicalColor.White },
        [new(2, 24)] = new() { Name = "Matthias the Apostle", Rank = FeastRank.Major, Color = LiturgicalColor.Red },

        // March
        [new(3, 19)] = new() { Name = "Joseph, the Guardian of Jesus", Rank = FeastRank.Major, Color = LiturgicalColor.White },
        [new(3, 25)] = new() { Name = "The Annunciation of Our Lord Jesus Christ to the Virgin Mary", Rank = FeastRank.Major, Color = LiturgicalColor.White },

        // April
        [new(4, 25)] = new() { Name = "Mark the Evangelist", Rank = FeastRank.Major, Color = LiturgicalColor.Red },

        // May
        [new(5, 1)] = new() { Name = "Philip and James, Apostles", Rank = FeastRank.Major, Color = LiturgicalColor.Red },
        [new(5, 31)] = new() { Name = "The Visitation of the Virgin Mary to Elizabeth and Zechariah", Rank = FeastRank.Major, Color = LiturgicalColor.White },

        // June
        [new(6, 11)] = new() { Name = "Barnabas the Apostle", Rank = FeastRank.Major, Color = LiturgicalColor.Red },
        [new(6, 24)] = new() { Name = "The Nativity of John the Baptist", Rank = FeastRank.Major, Color = LiturgicalColor.White },
        [new(6, 29)] = new() { Name = "Peter and Paul, Apostles", Rank = FeastRank.Major, Color = LiturgicalColor.Red },

        // July
        [new(7, 22)] = new() { Name = "Mary Magdalene", Rank = FeastRank.Major, Color = LiturgicalColor.White },
        [new(7, 25)] = new() { Name = "James the Elder, Apostle", Rank = FeastRank.Major, Color = LiturgicalColor.Red },

        // August
        [new(8, 6)] = new() { Name = "The Transfiguration of Our Lord Jesus Christ", Rank = FeastRank.Major, Color = LiturgicalColor.White },
        [new(8, 15)] = new() { Name = "The Virgin Mary, Mother of Our Lord Jesus Christ", Rank = FeastRank.Major, Color = LiturgicalColor.White },
        [new(8, 24)] = new() { Name = "Bartholomew the Apostle", Rank = FeastRank.Major, Color = LiturgicalColor.Red },

        // September
        [new(9, 14)] = new() { Name = "Holy Cross Day", Rank = FeastRank.Major, Color = LiturgicalColor.Red },
        [new(9, 21)] = new() { Name = "Matthew, Apostle and Evangelist", Rank = FeastRank.Major, Color = LiturgicalColor.Red },
        [new(9, 29)] = new() { Name = "Holy Michael and All Angels", Rank = FeastRank.Major, Color = LiturgicalColor.White },

        // October
        [new(10, 18)] = new() { Name = "Luke the Evangelist and Companion of Paul", Rank = FeastRank.Major, Color = LiturgicalColor.Red },
        [new(10, 23)] = new() { Name = "James of Jerusalem, Bishop and Martyr, Brother of Our Lord", Rank = FeastRank.Major, Color = LiturgicalColor.Red },
        [new(10, 28)] = new() { Name = "Simon and Jude, Apostles", Rank = FeastRank.Major, Color = LiturgicalColor.Red },

        // November
        [new(11, 1)] = new() { Name = "All Saints' Day", Rank = FeastRank.Principal, Color = LiturgicalColor.White },
        [new(11, 30)] = new() { Name = "Andrew the Apostle", Rank = FeastRank.Major, Color = LiturgicalColor.Red },

        // December
        [new(12, 21)] = new() { Name = "Thomas the Apostle", Rank = FeastRank.Major, Color = LiturgicalColor.Red },
        [new(12, 25)] = new() { Name = "Christmas Day", Rank = FeastRank.Principal, Color = LiturgicalColor.White },
        [new(12, 26)] = new() { Name = "Stephen, Deacon and Martyr", Rank = FeastRank.Major, Color = LiturgicalColor.Red },
        [new(12, 27)] = new() { Name = "John, Apostle and Evangelist", Rank = FeastRank.Major, Color = LiturgicalColor.White },
        [new(12, 28)] = new() { Name = "The Holy Innocents", Rank = FeastRank.Major, Color = LiturgicalColor.Red },
    };

    // ── Fixed Commemorations (rank below Major) ───────────────────────────────
    // A = Anglican optional commemoration (FeastRank.Optional)
    // E = Ecumenical optional commemoration (FeastRank.Commemoration)

    private static readonly Dictionary<MonthDay, List<FeastDay>> _fixedCommemorations = new()
    {
        // January
        [new(1, 2)] = [A("Vedanayagam Samuel Azariah, Bishop in South India, Evangelist, 1945", CommemorationCommon.MissionaryEvangelist)],
        [new(1, 10)] = [A("William Laud, Archbishop of Canterbury, Martyr, 1645", CommemorationCommon.Martyr)],
        [new(1, 13)] = [E("Hilary of Poitiers, Bishop and Teacher of the Faith, 367", CommemorationCommon.TeacherOfFaith)],
        [new(1, 14)] = [A("Kentigern, Missionary to Strathclyde and Cumbria, 603", CommemorationCommon.MissionaryEvangelist)],
        [new(1, 17)] = [E("Anthony, Hermit in Egypt, 356", CommemorationCommon.MonasticReligious)],
        [new(1, 19)] = [A("Wulfstan, Bishop of Worcester, 1095", CommemorationCommon.Pastor)],
        [new(1, 20)] = [E("Fabian, Bishop of Rome and Martyr, 250", CommemorationCommon.Martyr)],
        [new(1, 21)] = [E("Agnes, Martyr at Rome, 304", CommemorationCommon.Martyr)],
        [new(1, 22)] = [E("Vincent, Deacon of Saragossa, Martyr, 304", CommemorationCommon.Martyr)],
        [new(1, 26)] = [E("Timothy and Titus, Companions of Paul the Apostle", CommemorationCommon.MissionaryEvangelist)],
        [new(1, 27)] = [E("Lydia, Dorcas and Phoebe, Helpers of the Apostles", CommemorationCommon.AnyCommemorationI)],
        [new(1, 28)] = [E("Thomas Aquinas, Friar, Priest, and Teacher of the Faith, 1274", CommemorationCommon.TeacherOfFaith)],
        [new(1, 29)] = [A("Lesslie Newbigin, Bishop and Ecumenist, 1998", CommemorationCommon.Ecumenist)],
        [new(1, 30)] = [A("Charles, King and Martyr, 1649", CommemorationCommon.Martyr)],
        [new(1, 31)] = [A("Samuel Shoemaker, Priest and Renewer of Society, 1963", CommemorationCommon.RenewerOfSociety)],

        // February
        [new(2, 1)] = [A("Brigid, Abbess of Kildare, 523", CommemorationCommon.MonasticReligious)],
        [new(2, 3)] = [E("Anskar, Bishop and Missionary to Denmark and Sweden, 865", CommemorationCommon.MissionaryEvangelist)],
        [new(2, 4)] = [E("Cornelius the Centurion", CommemorationCommon.AnyCommemorationI)],
        [new(2, 5)] = [E("Martyrs of Japan, 1597", CommemorationCommon.Martyr)],
        [new(2, 10)] = [E("Scholastica, Abbess, 543", CommemorationCommon.MonasticReligious)],
        [new(2, 13)] = [A("Absalom Jones, First African-American Priest, 1818", CommemorationCommon.Pastor)],
        [new(2, 14)] = [E("Cyril and Methodius, Apostles to the Slavs, 869, 885", CommemorationCommon.MissionaryEvangelist)],
        [new(2, 15)] = [A("Thomas Bray, Priest and Missionary, Founder of SPG and SPCK, 1730", CommemorationCommon.MissionaryEvangelist)],
        [new(2, 17)] = [A("Janani Luwum, Archbishop of Uganda and Martyr, 1977", CommemorationCommon.Martyr)],
        [new(2, 18)] = [E("Martin Luther, Reformer of the Church, 1546", CommemorationCommon.ReformerOfChurch)],
        [new(2, 21)] = [E("William \"Billy\" Graham, Evangelist, 2018", CommemorationCommon.MissionaryEvangelist)],
        [new(2, 23)] = [E("Polycarp, Bishop of Smyrna, Martyr, 156", CommemorationCommon.Martyr)],
        [new(2, 27)] = [A("George Herbert, Priest and Poet, 1633", CommemorationCommon.TeacherOfFaith)],
        [new(2, 28)] = [E("John Cassian, Monk and Teacher of the Faith, 453", CommemorationCommon.TeacherOfFaith)],

        // March
        [new(3, 1)] = [A("David, Bishop and Apostle of Wales, 601", CommemorationCommon.MissionaryEvangelist)],
        [new(3, 2)] = [A("Chad, Bishop of Lichfield and Missionary, 672", CommemorationCommon.MissionaryEvangelist)],
        [new(3, 3)] = [A("John and Charles Wesley, Priests and Reformers of the Church, 1791, 1788", CommemorationCommon.ReformerOfChurch)],
        [new(3, 7)] = [E("Perpetua and Her Companions, Martyrs at Carthage, 203", CommemorationCommon.Martyr)],
        [new(3, 8)] = [A("Felix, Bishop and Missionary to the Angles, 647", CommemorationCommon.MissionaryEvangelist)],
        [new(3, 10)] = [A("Robert Machray, First Primate of Canada, 1904", CommemorationCommon.Pastor)],
        [new(3, 12)] = [E("Gregory the Great, Bishop of Rome and Teacher of the Faith, 604", CommemorationCommon.TeacherOfFaith)],
        [new(3, 17)] = [A("Patrick, Bishop and Apostle to the Irish, 461", CommemorationCommon.MissionaryEvangelist)],
        [new(3, 18)] = [E("Cyril, Bishop of Jerusalem and Teacher of the Faith, 386", CommemorationCommon.TeacherOfFaith)],
        [new(3, 20)] = [A("Cuthbert, Bishop-Abbot of Lindisfarne and Missionary, 687", CommemorationCommon.MissionaryEvangelist)],
        [new(3, 21)] = [A("Thomas Cranmer, Archbishop of Canterbury and Martyr, 1556", CommemorationCommon.Martyr)],
        [new(3, 22)] = [A("James DeKoven, Priest, 1879", CommemorationCommon.Pastor)],
        [new(3, 23)] = [E("Gregory the Illuminator, Missionary to Armenia, 333", CommemorationCommon.MissionaryEvangelist)],
        [new(3, 27)] = [A("Charles Henry Brent, Bishop and Missionary to the Philippines, 1929", CommemorationCommon.MissionaryEvangelist)],
        [new(3, 29)] = [A("John Keble, Priest and Reformer of the Church, 1866", CommemorationCommon.ReformerOfChurch)],
        [new(3, 31)] = [A("John Donne, Priest and Poet, 1631", CommemorationCommon.TeacherOfFaith)],

        // April
        [new(4, 1)] = [A("Frederick Denison Maurice, Priest and Renewer of Society, 1872", CommemorationCommon.RenewerOfSociety)],
        [new(4, 2)] = [A("Henry Budd, First Native Priest in Canada, 1850", CommemorationCommon.Pastor)],
        [new(4, 3)] = [A("James Lloyd Breck, Priest and Missionary, 1876", CommemorationCommon.MissionaryEvangelist)],
        [new(4, 4)] = [E("Martin Luther King, Jr., Renewer of Society, 1968", CommemorationCommon.RenewerOfSociety)],
        [new(4, 7)] = [E("Tikhon, Bishop and Ecumenist, 1925", CommemorationCommon.Ecumenist)],
        [new(4, 8)] = [A("William Augustus Muhlenberg, Priest, Reformer of the Church and Renewer of Society, 1877", CommemorationCommon.RenewerOfSociety)],
        [new(4, 10)] = [A("William Law, Priest and Teacher of the Faith, 1761", CommemorationCommon.TeacherOfFaith)],
        [new(4, 11)] = [A("George Augustus Selwyn, Bishop and Missionary to New Zealand, 1878", CommemorationCommon.MissionaryEvangelist)],
        [new(4, 19)] = [A("Alphege, Archbishop of Canterbury and Martyr, 1012", CommemorationCommon.Martyr)],
        [new(4, 21)] = [A("Anselm, Archbishop of Canterbury and Teacher of the Faith, 1109", CommemorationCommon.TeacherOfFaith)],
        [new(4, 23)] = [E("George, Martyr, c. 304", CommemorationCommon.Martyr)],
        [new(4, 24)] = [A("Arthur Michael Ramsey, Archbishop of Canterbury, Ecumenist and Teacher of the Faith, 1988", CommemorationCommon.Ecumenist)],
        [new(4, 29)] = [E("Catherine of Siena, Reformer of the Church, 1380", CommemorationCommon.ReformerOfChurch)],

        // May
        [new(5, 2)] = [E("Athanasius, Bishop of Alexandria and Teacher of the Faith, 373", CommemorationCommon.TeacherOfFaith)],
        [new(5, 8)] = [A("Julian of Norwich, Anchoress, c. 1417", CommemorationCommon.MonasticReligious)],
        [new(5, 9)] = [E("Gregory of Nazianzus, Bishop of Constantinople and Teacher of the Faith, 389", CommemorationCommon.TeacherOfFaith)],
        [new(5, 15)] = [E("Pachomius, Abbot and Organizer of Monasticism, 346", CommemorationCommon.MonasticReligious)],
        [new(5, 16)] = [A("The Martyrs of the Sudan, 2011", CommemorationCommon.Martyr)],
        [new(5, 19)] = [A("Dunstan, Archbishop of Canterbury and Reformer of the Church, 988", CommemorationCommon.ReformerOfChurch)],
        [new(5, 20)] = [A("Alcuin, Deacon and Abbot of Tours, 804", CommemorationCommon.MonasticReligious)],
        [new(5, 21)] = [E("Helena, Mother of Constantine, Protector of the Holy Places, 330", CommemorationCommon.AnyCommemorationI)],
        [new(5, 24)] = [A("Jackson Kemper, First Missionary Bishop in the United States, 1870", CommemorationCommon.MissionaryEvangelist)],
        [new(5, 25)] = [A("Bede the Venerable, Priest and Monk of Jarrow, Teacher of the Faith, 735", CommemorationCommon.TeacherOfFaith)],
        [new(5, 26)] = [A("Augustine, First Archbishop of Canterbury and Missionary, 605", CommemorationCommon.MissionaryEvangelist)],
        [new(5, 27)] = [E("John Calvin, Reformer of the Church, 1564", CommemorationCommon.ReformerOfChurch)],
        [new(5, 30)] = [A("Josephine Butler, Renewer of Society, 1906", CommemorationCommon.RenewerOfSociety)],

        // June
        [new(6, 1)] = [E("Justin, Teacher of the Faith and Martyr at Rome, c. 165", CommemorationCommon.Martyr)],
        [new(6, 2)] = [E("Blandina and Her Companions, Martyrs at Lyons, 177", CommemorationCommon.Martyr)],
        [new(6, 3)] = [A("The Martyrs of Uganda, 1886, 1977", CommemorationCommon.Martyr)],
        [new(6, 4)] = [E("John XXIII, Bishop of Rome, Ecumenist and Reformer of the Church, 1963", CommemorationCommon.Ecumenist)],
        [new(6, 5)] = [A("Boniface, Archbishop of Mainz, Missionary to the Germans and Martyr, 754", CommemorationCommon.Martyr)],
        [new(6, 6)] = [A("William Grant Broughton, Bishop and Missionary to Australia, 1853", CommemorationCommon.MissionaryEvangelist)],
        [new(6, 8)] = [A("Thomas Ken, Bishop of Bath and Wells, Non-juror, 1711", CommemorationCommon.Pastor)],
        [new(6, 9)] = [A("Columba, Abbot of Iona and Missionary to the Scots, 597", CommemorationCommon.MissionaryEvangelist)],
        [new(6, 10)] = [E("Ephrem of Edessa, Deacon and Teacher of the Faith, 373", CommemorationCommon.TeacherOfFaith)],
        [new(6, 14)] = [E("Basil the Great, Bishop of Caesarea and Teacher of the Faith, 379", CommemorationCommon.TeacherOfFaith)],
        [new(6, 15)] = [A("Evelyn Underhill, Teacher of the Faith, 1941", CommemorationCommon.TeacherOfFaith)],
        [new(6, 18)] = [A("Bernard Mizeki, Catechist and Martyr in Rhodesia, 1896", CommemorationCommon.Martyr)],
        [new(6, 19)] = [A("Sundar Singh, Evangelist in India and Teacher of the Faith, 1929", CommemorationCommon.MissionaryEvangelist)],
        [new(6, 22)] = [A("Alban, First Martyr of Britain, c. 250", CommemorationCommon.Martyr)],
        [new(6, 27)] = [E("Cyril of Alexandria, Bishop and Teacher of the Faith, 444", CommemorationCommon.TeacherOfFaith)],
        [new(6, 28)] = [E("Irenaeus, Bishop of Lyons and Teacher of the Faith, 200", CommemorationCommon.TeacherOfFaith)],

        // July
        [new(7, 11)] = [E("Benedict of Nursia, Abbot and Founder of the Benedictine Order, c. 550", CommemorationCommon.MonasticReligious)],
        [new(7, 12)] = [E("Nathan Soderblom, Archbishop of Uppsala and Ecumenist, 1931", CommemorationCommon.Ecumenist)],
        [new(7, 14)] = [E("Bonaventure, Friar, Bishop and Teacher of the Faith, 1274", CommemorationCommon.TeacherOfFaith)],
        [new(7, 15)] = [E("Olga and Vladimir, Patrons of the Church in Russia, 969, 1016", CommemorationCommon.AnyCommemorationI)],
        [new(7, 17)] = [A("William White, Bishop of Pennsylvania and First Presiding Bishop of the Church in the USA, 1836", CommemorationCommon.Pastor)],
        [new(7, 18)] = [E("Macrina, Nun and Teacher of the Faith, 379", CommemorationCommon.MonasticReligious)],
        [new(7, 19)] = [E("Gregory, Bishop of Nyssa and Teacher of the Faith, 396", CommemorationCommon.TeacherOfFaith)],
        [new(7, 20)] = [E("Margaret of Antioch, Martyr, 4th c.", CommemorationCommon.Martyr)],
        [new(7, 24)] = [E("Thomas à Kempis, Priest and Teacher of the Faith, 1471", CommemorationCommon.TeacherOfFaith)],
        [new(7, 26)] = [E("The Parents of the Virgin Mary", CommemorationCommon.AnyCommemorationI)],
        [new(7, 27)] = [A("William Reed Huntington, Priest and Ecumenist, 1909", CommemorationCommon.Ecumenist)],
        [new(7, 29)] = [E("Lazarus, Mary and Martha of Bethany, Companions of Our Lord", CommemorationCommon.AnyCommemorationI)],
        [new(7, 30)] = [A("William Wilberforce, Renewer of Society, 1833", CommemorationCommon.RenewerOfSociety)],

        // August
        [new(8, 1)] = [E("Joseph of Arimathea", CommemorationCommon.AnyCommemorationI)],
        [new(8, 5)] = [A("Oswald, King of Northumbria and Martyr, 642", CommemorationCommon.Martyr)],
        [new(8, 7)] = [A("John Mason Neale, Priest and Reformer of the Church, 1866", CommemorationCommon.ReformerOfChurch)],
        [new(8, 8)] = [E("Dominic, Priest and Friar, 1221", CommemorationCommon.MonasticReligious)],
        [new(8, 9)] = [A("Mary Sumner, Founder of the Mothers' Union and Renewer of Society, 1921", CommemorationCommon.RenewerOfSociety)],
        [new(8, 10)] = [E("Laurence, Deacon and Martyr at Rome, 258", CommemorationCommon.Martyr)],
        [new(8, 11)] = [E("Clare, Abbess of Assisi, 1253", CommemorationCommon.MonasticReligious)],
        [new(8, 12)] = [A("Charles Inglis, First Bishop of Canada, 1787", CommemorationCommon.Pastor)],
        [new(8, 13)] = [A("Jeremy Taylor, Bishop of Down and Connor, Teacher of the Faith, 1667", CommemorationCommon.TeacherOfFaith)],
        [new(8, 14)] = [E("Roger Schutz, Monk of Taizé and Ecumenist, 2005", CommemorationCommon.Ecumenist)],
        [new(8, 20)] = [E("Bernard, Abbot of Clairvaux and Teacher of the Faith, 1153", CommemorationCommon.TeacherOfFaith)],
        [new(8, 21)] = [A("Jonathan Myrick Daniels, Martyr, 1965", CommemorationCommon.Martyr)],
        [new(8, 25)] = [E("Louis, King of France, 1270", CommemorationCommon.AnyCommemorationI)],
        [new(8, 27)] = [E("Monica, Mother of Augustine of Hippo, 387", CommemorationCommon.AnyCommemorationI)],
        [new(8, 28)] = [E("Augustine, Bishop of Hippo and Teacher of the Faith, 430", CommemorationCommon.TeacherOfFaith)],
        [new(8, 29)] = [E("The Beheading of John the Baptist", CommemorationCommon.AnyCommemorationI)],
        [new(8, 30)] = [A("Charles Chapman Grafton, Bishop of Fond du Lac and Ecumenist, 1912", CommemorationCommon.Ecumenist)],
        [new(8, 31)] = [A("Aidan, Abbot-Bishop of Lindisfarne, Missionary to Northumbria, 651", CommemorationCommon.MissionaryEvangelist)],

        // September
        [new(9, 2)] = [A("The Martyrs of Papua New Guinea, 1901 and 1942", CommemorationCommon.Martyr)],
        [new(9, 4)] = [A("Birinus, Bishop of Dorchester and Evangelist to Wessex, 650", CommemorationCommon.MissionaryEvangelist)],
        [new(9, 5)] = [E("Mother Teresa of Calcutta, Renewer of Society, 1997", CommemorationCommon.RenewerOfSociety)],
        [new(9, 6)] = [A("Allen Gardiner, Missionary and Founder of SAMS, 1851", CommemorationCommon.MissionaryEvangelist)],
        [new(9, 7)] = [A("Hannah More, Renewer of Society and Founder of Sunday Schools, 1833", CommemorationCommon.RenewerOfSociety)],
        [new(9, 9)] = [A("Constance and Her Companions, Martyrs of Memphis, 1878", CommemorationCommon.Martyr)],
        [new(9, 10)] = [A("Alexander Crummell, Priest and Missionary to Liberia, 1898", CommemorationCommon.MissionaryEvangelist)],
        [new(9, 12)] = [A("John Henry Hobart, Bishop of New York and Reformer of the Church, 1830", CommemorationCommon.ReformerOfChurch)],
        [new(9, 13)] = [E("John Chrysostom, Bishop of Constantinople and Teacher of the Faith, 407", CommemorationCommon.TeacherOfFaith)],
        [new(9, 15)] = [E("Cyprian, Bishop of Carthage and Martyr, 258", CommemorationCommon.Martyr)],
        [new(9, 16)] = [A("Ninian, Bishop of Galloway and Missionary to the Picts, 432", CommemorationCommon.MissionaryEvangelist)],
        [new(9, 17)] = [A("Edward Bouverie Pusey, Priest and Teacher of the Faith, 1882", CommemorationCommon.TeacherOfFaith)],
        [new(9, 19)] = [A("Theodore of Tarsus, Archbishop of Canterbury, 690", CommemorationCommon.Pastor)],
        [new(9, 20)] = [A("John Coleridge Patteson, Bishop of Melanesia and His Companions, Martyrs, 1871", CommemorationCommon.Martyr)],
        [new(9, 25)] = [E("Sergius, Monk and Reformer of the Church in Russia, 1392", CommemorationCommon.MonasticReligious)],
        [new(9, 26)] = [A("Lancelot Andrewes, Bishop of Winchester and Teacher of the Faith, 1626", CommemorationCommon.TeacherOfFaith)],
        [new(9, 27)] = [A("Wilson Carlile, Evangelist and Founder of the Church Army, 1942", CommemorationCommon.MissionaryEvangelist)],
        [new(9, 30)] = [E("Jerome, Monk of Bethlehem and Translator of the Bible, 420", CommemorationCommon.TeacherOfFaith)],

        // October
        [new(10, 1)] = [E("Remigius, Bishop of Reims and Missionary to the Franks, 533", CommemorationCommon.MissionaryEvangelist)],
        [new(10, 3)] = [A("George Bell, Advocate for the Confessing Church, Bishop and Ecumenist, 1958", CommemorationCommon.Ecumenist)],
        [new(10, 4)] = [E("Francis of Assisi, Friar and Deacon, Reformer of the Church, 1226", CommemorationCommon.MonasticReligious)],
        [new(10, 6)] = [A("William Tyndale, Priest, Translator of the Bible and Martyr, 1536", CommemorationCommon.Martyr)],
        [new(10, 9)] = [A("Robert Grosseteste, Bishop of Lincoln, 1253", CommemorationCommon.Pastor)],
        [new(10, 10)] = [A("Paulinus, Bishop of York and Missionary, 644", CommemorationCommon.MissionaryEvangelist)],
        [new(10, 11)] = [E("Philip, Deacon and Evangelist", CommemorationCommon.MissionaryEvangelist)],
        [new(10, 12)] = [A("Cecil Frances Alexander, Hymn-writer and Teacher of the Faith, 1895", CommemorationCommon.TeacherOfFaith)],
        [new(10, 13)] = [A("Edward the Confessor, King of England, 1066", CommemorationCommon.AnyCommemorationI)],
        [new(10, 14)] = [A("Samuel Isaac Joseph Schereschewsky, Bishop of Shanghai, 1906", CommemorationCommon.MissionaryEvangelist)],
        [new(10, 15)] = [E("Teresa of Ávila, Nun and Reformer of the Church, 1582", CommemorationCommon.MonasticReligious)],
        [new(10, 16)] = [A("Hugh Latimer and Nicholas Ridley, Bishops and Martyrs, 1555", CommemorationCommon.Martyr)],
        [new(10, 17)] = [E("Ignatius, Bishop of Antioch and Martyr, c. 115", CommemorationCommon.Martyr)],
        [new(10, 19)] = [A("Henry Martyn, Priest and Missionary to India and Persia, 1812", CommemorationCommon.MissionaryEvangelist)],
        [new(10, 26)] = [A("Alfred the Great, King of the West Saxons and Reformer of the Church, 899", CommemorationCommon.ReformerOfChurch)],
        [new(10, 29)] = [A("James Hannington, Bishop of Eastern Equatorial Africa and His Companions, Martyrs, 1885", CommemorationCommon.Martyr)],

        // November
        [new(11, 2)] = [E("Commemoration of the Faithful Departed (All Souls' Day)", CommemorationCommon.AnyCommemorationI)],
        [new(11, 3)] = [A("Richard Hooker, Priest and Teacher of the Faith, 1600", CommemorationCommon.TeacherOfFaith)],
        [new(11, 5)] = [E("Elizabeth and Zechariah, Parents of John the Baptist", CommemorationCommon.AnyCommemorationI)],
        [new(11, 6)] = [A("William Temple, Archbishop of Canterbury and Teacher of the Faith, 1944", CommemorationCommon.TeacherOfFaith)],
        [new(11, 7)] = [E("Willibrord, Archbishop of Utrecht and Missionary to Frisia, 739", CommemorationCommon.MissionaryEvangelist)],
        [new(11, 10)] = [E("Leo the Great, Bishop of Rome and Teacher of the Faith, 461", CommemorationCommon.TeacherOfFaith)],
        [new(11, 11)] = [E("Martin, Bishop of Tours, 397", CommemorationCommon.Pastor)],
        [new(11, 13)] = [A("Charles Simeon, Priest and Evangelist, 1836", CommemorationCommon.MissionaryEvangelist)],
        [new(11, 14)] = [A("Consecration of Samuel Seabury, First Bishop in the United States, 1784", CommemorationCommon.Pastor)],
        [new(11, 15)] = [E("Herman, Monk and Missionary to the Native Alaskans, 1837", CommemorationCommon.MissionaryEvangelist)],
        [new(11, 16)] = [A("Margaret, Queen of Scotland, Reformer of the Church and Renewer of Society, 1093", CommemorationCommon.RenewerOfSociety)],
        [new(11, 17)] = [A("Hugh, Bishop of Lincoln and Renewer of Society, 1200", CommemorationCommon.RenewerOfSociety)],
        [new(11, 18)] = [E("Elizabeth of Hungary, Renewer of Society, 1231", CommemorationCommon.RenewerOfSociety)],
        [new(11, 19)] = [A("Hilda, Abbess of Whitby, 680", CommemorationCommon.MonasticReligious)],
        [new(11, 20)] = [A("Edmund, King of East Anglia and Martyr, 870", CommemorationCommon.Martyr)],
        [new(11, 22)] = [E("Cecilia, Martyr at Rome, c. 230", CommemorationCommon.Martyr)],
        [new(11, 23)] = [E("Clement, Bishop of Rome and Martyr, c. 100", CommemorationCommon.Martyr)],
        [new(11, 25)] = [E("Catherine of Alexandria, Martyr, c. 305", CommemorationCommon.Martyr)],
        [new(11, 29)] = [A("Clive Staples Lewis, Teacher of the Faith, 1963", CommemorationCommon.TeacherOfFaith)],

        // December
        [new(12, 1)] = [A("Nicholas Ferrar, Deacon and Founder of the Little Gidding Community, 1637", CommemorationCommon.MonasticReligious)],
        [new(12, 2)] = [A("Channing Moore Williams, Missionary Bishop in China and Japan, 1910", CommemorationCommon.MissionaryEvangelist)],
        [new(12, 4)] = [E("John of Damascus, Priest and Teacher of the Faith, 760", CommemorationCommon.TeacherOfFaith)],
        [new(12, 5)] = [E("Clement of Alexandria, Priest and Teacher of the Faith, 210", CommemorationCommon.TeacherOfFaith)],
        [new(12, 6)] = [E("Nicholas, Bishop of Myra, c. 326", CommemorationCommon.Pastor)],
        [new(12, 7)] = [E("Ambrose, Bishop of Milan and Teacher of the Faith, 397", CommemorationCommon.TeacherOfFaith)],
        [new(12, 8)] = [A("Richard Baxter, Pastor and Teacher of the Faith, 1691", CommemorationCommon.TeacherOfFaith)],
        [new(12, 13)] = [E("Lucy, Martyr at Syracuse, 304", CommemorationCommon.Martyr)],
        [new(12, 29)] = [A("Thomas Becket, Archbishop of Canterbury, Martyr, 1170", CommemorationCommon.Martyr)],
        [new(12, 31)] = [A("John Wyclif, Priest and Translator of the Bible into English, 1384", CommemorationCommon.TeacherOfFaith)],
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FeastDay A(string name, CommemorationCommon common) =>
        new() { Name = name, Rank = FeastRank.Optional, Common = common };

    private static FeastDay E(string name, CommemorationCommon common) =>
        new() { Name = name, Rank = FeastRank.Commemoration, Common = common };

    /// <summary>
    /// Returns true if <paramref name="date"/> is the Wednesday, Friday, or Saturday
    /// that immediately follows <paramref name="anchor"/>.
    /// </summary>
    private static bool IsEmberDayAfterAnchor(DateOnly date, DateOnly anchor)
    {
        // Find the next Wednesday strictly after the anchor
        int daysToWed = (((int)DayOfWeek.Wednesday - (int)anchor.DayOfWeek + 7) % 7);
        if (daysToWed == 0)
        {
            daysToWed = 7;
        }

        var wednesday = anchor.AddDays(daysToWed);
        return date == wednesday || date == wednesday.AddDays(2) || date == wednesday.AddDays(3);
    }
}
