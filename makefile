
SLN := Cypherly.Ledger.sln

.PHONY: restore build test clean

restore:
	dotnet restore $(SLN)

build: restore
	dotnet build $(SLN) --no-restore

test: build
	dotnet test $(SLN) --no-build

clean:
	dotnet clean $(SLN)
