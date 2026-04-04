$c = Get-Content 'README.md' -Raw

$insertBefore = '**Step 5'

$screenshot = "`n> **Seq Trace Waterfall** After placing an order, open [http://localhost:8081](http://localhost:8081), switch to the Traces view, and filter by CorrelationId to see the full distributed trace:`n>`n> ![Seq Dashboard](docs/assets/seq-dashboard-placeholder.svg)`n>`n> *Replace docs/assets/seq-dashboard-placeholder.svg with a real screenshot (docs/assets/seq-dashboard.png) and update this reference.*`n`n"

$c2 = $c.Replace($insertBefore, $screenshot + $insertBefore)

Set-Content 'README.md' -Value $c2 -NoNewline

Write-Host 'Done - screenshot placeholder inserted'
