$asm = [Reflection.Assembly]::LoadFrom((Resolve-Path '.\dist\ReptileDesktopPet.exe'))
$modelType = $asm.GetType('ReptileDesktopPet.CreatureModel', $true)
$vecType = $asm.GetType('ReptileDesktopPet.Vec', $true)
$model = [Activator]::CreateInstance($modelType, [object[]]@(0))

function New-TestVec([double]$x, [double]$y) {
    [Activator]::CreateInstance($vecType, [object[]]@($x, $y))
}

$reset = $modelType.GetMethod('Reset')
$update = $modelType.GetMethod('Update')
$reset.Invoke($model, [object[]]@((New-TestVec 200 500), 1.0)) | Out-Null
$xField = $modelType.BaseType.GetField('X')
$yField = $modelType.BaseType.GetField('Y')
$angleField = $modelType.BaseType.GetField('AbsAngle')
$maxYError = 0.0

for ($i = 1; $i -le 90; $i++) {
    $targetX = 200 + $i * 10
    $update.Invoke($model, [object[]]@((New-TestVec $targetX 500), [double](1.0 / 60.0), 1.0)) | Out-Null
    $rootY = [double]$yField.GetValue($model)
    $maxYError = [Math]::Max($maxYError, [Math]::Abs($rootY - 500))
}

for ($i = 0; $i -lt 180; $i++) {
    $update.Invoke($model, [object[]]@((New-TestVec 1100 500), [double](1.0 / 60.0), 1.0)) | Out-Null
}

function Normalize-TestAngle([double]$angle) {
    $angle - 2 * [Math]::PI * [Math]::Floor($angle / (2 * [Math]::PI) + 0.5)
}

$spineField = $modelType.GetField('_spine', [Reflection.BindingFlags]'Instance,NonPublic')
$spine = $spineField.GetValue($model)
$angles = @()
foreach ($segment in $spine) {
    $angles += [double]$segment.GetType().GetField('AbsAngle').GetValue($segment)
}

$rootAngle = [double]$angleField.GetValue($model)
$rear = $rootAngle + [Math]::PI
$offsets = @($angles | ForEach-Object { Normalize-TestAngle ($_ - $rear) })
$localBends = @()
for ($i = 1; $i -lt $angles.Count; $i++) {
    $localBends += Normalize-TestAngle ($angles[$i] - $angles[$i - 1])
}
$signChanges = 0
for ($i = 1; $i -lt $localBends.Count; $i++) {
    if ($localBends[$i] * $localBends[$i - 1] -lt 0) { $signChanges++ }
}

[pscustomobject]@{
    MaxStraightLineYError = [Math]::Round($maxYError, 6)
    RootX = [Math]::Round([double]$xField.GetValue($model), 3)
    RootY = [Math]::Round([double]$yField.GetValue($model), 3)
    Sleeping = $modelType.GetProperty('IsSleeping').GetValue($model, $null)
    MaxSpineOffsetRad = [Math]::Round(($offsets | ForEach-Object { [Math]::Abs($_) } | Measure-Object -Maximum).Maximum, 4)
    MaxJointBendRad = [Math]::Round(($localBends | ForEach-Object { [Math]::Abs($_) } | Measure-Object -Maximum).Maximum, 4)
    BendSignChanges = $signChanges
    SpineSegments = $angles.Count
} | Format-List
