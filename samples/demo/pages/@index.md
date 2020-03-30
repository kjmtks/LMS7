# @Model.Lecture.Subject


This system is the novel learning management system (LMS) with the following features:

* This system can build sandbox environments, and provide programming exercises executing in them environments.
* Educational contents can be described in the markdown format, and uploaded / downloaded via Git interface.
* Submitted programs can be automatically verified for suitability using test cases.


<script language="activity" ref="ruby.xml">
  <Name>act01</Name>
  <Subject>Hello, World!!</Subject>
  <Deadline>@Model.DateTimeToString(ViewBag.week1start)</Deadline>
  <Description>Create a Ruby program that outputs `Hello, World!!`.</Description>
  <Answer>puts "Hello, World!!"</Answer>
  <ExpectedStdout>Hello, World!!</ExpectedStdout>
</script>

<script language="activity" ref="ruby.xml">
  <Name>act02</Name>
  <Subject>Bye, World!!</Subject>
  <Deadline>@Model.DateTimeToString(ViewBag.week1start)</Deadline>
  <Description>Create a Ruby program that outputs `Bye, World!!`.</Description>
  <Answer>puts "Bye, World!!"</Answer>
  <ExpectedStdout>Bye, World!!</ExpectedStdout>
</script>

<script language="activity" ref="upload.xml">
  <Name>act03</Name>
  <Subject>Upload PDF file</Subject>
  <Deadline>@Model.DateTimeToString(ViewBag.week1start)</Deadline>
  <Description>Create a report.</Description>
  <FileName>report.pdf</FileName>
  <Label>Report (PDF file)</Label>
  <ContentTypes>application/pdf;</ContentTypes>
  <MaxSize>@(1024*1024*1024)</MaxSize>
</script>

<script language="activity" ref="form.xml">
  <Name>act04</Name>
  <Subject>Check</Subject>
  <Deadline>@Model.DateTimeToString(ViewBag.week1start)</Deadline>
  <Description>Answer following questions.</Description>
</script>

[dir/@@page2.md](dir/@@page2.md)
