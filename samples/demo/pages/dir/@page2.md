
<script language="activity" ref="ruby.xml">
  <Name>ACT2</Name>
  <Subject><TEST></Subject>
  <Deadline>@Model.DateTimeToString(ViewBag.week1start)</Deadline>
  <Description>ACT2</Description>
</script>

<a href="@Model.To("@page2.md")">@@page2</a>
| <a href="@Model.To("./@page2.md")">@@page2</a>
| <a href="@Model.To("../@index.md")">@@index</a>
| <a href="@Model.To("/@index.md")">@@index</a>
