#!/bin/bash

# Build and Test Script for JobService
# This script builds the entire solution, runs unit tests, and generates test reports

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SOLUTION_FILE="JobService.sln"
SERVER_PROJECT="JobService.Server/JobService.Server.csproj"
CLIENT_PROJECT="JobService.Client/JobService.Client.csproj"
TEST_PROJECT="JobService.Client.Tests/JobService.Client.Tests.csproj"
REPORTS_DIR="TestReports"
COVERAGE_DIR="CoverageReports"

# Functions
print_header() {
    echo -e "${BLUE}================================================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}================================================================${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

check_prerequisites() {
    print_header "Checking Prerequisites"
    
    # Check if dotnet is installed
    if ! command -v dotnet &> /dev/null; then
        print_error "dotnet CLI is not installed"
        exit 1
    fi
    
    local dotnet_version=$(dotnet --version)
    print_success "dotnet CLI version: $dotnet_version"
    
    # Check if solution file exists
    if [ ! -f "$SOLUTION_FILE" ]; then
        print_error "Solution file $SOLUTION_FILE not found"
        exit 1
    fi
    
    print_success "Solution file found: $SOLUTION_FILE"
    echo
}

clean_solution() {
    print_header "Cleaning Solution"
    
    dotnet clean "$SOLUTION_FILE" --verbosity minimal
    
    # Remove previous reports
    if [ -d "$REPORTS_DIR" ]; then
        rm -rf "$REPORTS_DIR"
        print_info "Removed previous test reports"
    fi
    
    if [ -d "$COVERAGE_DIR" ]; then
        rm -rf "$COVERAGE_DIR"
        print_info "Removed previous coverage reports"
    fi
    
    print_success "Solution cleaned successfully"
    echo
}

restore_packages() {
    print_header "Restoring NuGet Packages"
    
    dotnet restore "$SOLUTION_FILE" --verbosity minimal
    
    print_success "Packages restored successfully"
    echo
}

build_solution() {
    print_header "Building Solution"
    
    dotnet build "$SOLUTION_FILE" --configuration Release --no-restore --verbosity minimal
    
    print_success "Solution built successfully"
    echo
}

run_tests() {
    print_header "Running Unit Tests"
    
    # Create reports directory
    mkdir -p "$REPORTS_DIR"
    mkdir -p "$COVERAGE_DIR"
    
    local test_results_file="$REPORTS_DIR/test-results.trx"
    local coverage_file="$COVERAGE_DIR/coverage.cobertura.xml"
    local coverage_html_dir="$COVERAGE_DIR/html"
    
    print_info "Running tests with coverage collection..."
    
    # Run tests with coverage and detailed reporting
    dotnet test "$TEST_PROJECT" \
        --configuration Release \
        --verbosity normal \
        --logger "trx;LogFileName=test-results.trx" \
        --results-directory "$REPORTS_DIR" \
        --collect:"XPlat Code Coverage"
    
    # Move coverage files to our coverage directory
    local coverage_source=$(find . -name "coverage.cobertura.xml" -type f | head -n 1)
    if [ -n "$coverage_source" ]; then
        cp "$coverage_source" "$coverage_file"
        print_success "Coverage report generated: $coverage_file"
    else
        print_warning "Coverage report not found"
    fi
    
    print_success "Unit tests completed"
    echo
}

generate_coverage_html() {
    print_header "Generating HTML Coverage Report"
    
    # Check if reportgenerator tool is installed
    if ! dotnet tool list -g | grep -q reportgenerator; then
        print_info "Installing ReportGenerator tool..."
        dotnet tool install -g dotnet-reportgenerator-globaltool
    fi
    
    local coverage_file="$COVERAGE_DIR/coverage.cobertura.xml"
    local coverage_html_dir="$COVERAGE_DIR/html"
    
    if [ -f "$coverage_file" ]; then
        mkdir -p "$coverage_html_dir"
        
        reportgenerator \
            "-reports:$coverage_file" \
            "-targetdir:$coverage_html_dir" \
            "-reporttypes:Html;HtmlSummary" \
            "-title:JobService Coverage Report"
        
        print_success "HTML coverage report generated: $coverage_html_dir/index.html"
    else
        print_warning "Coverage file not found, skipping HTML report generation"
    fi
    
    echo
}

create_coverlet_settings() {
    # Settings are now handled inline with the test command
    print_info "Coverage settings will be applied during test execution"
}

generate_test_summary() {
    print_header "Test Summary"
    
    local test_results_file="$REPORTS_DIR/test-results.trx"
    
    if [ -f "$test_results_file" ]; then
        # Extract test summary from TRX file
        local total_tests=$(grep -o 'total="[0-9]*"' "$test_results_file" | grep -o '[0-9]*' | head -n 1)
        local passed_tests=$(grep -o 'passed="[0-9]*"' "$test_results_file" | grep -o '[0-9]*' | head -n 1)
        local failed_tests=$(grep -o 'failed="[0-9]*"' "$test_results_file" | grep -o '[0-9]*' | head -n 1)
        
        # Default to 0 if not found
        total_tests=${total_tests:-0}
        passed_tests=${passed_tests:-0}
        failed_tests=${failed_tests:-0}
        
        echo "📊 Test Results:"
        echo "   Total Tests: $total_tests"
        echo "   Passed: $passed_tests"
        echo "   Failed: $failed_tests"
        
        if [ "$failed_tests" -eq 0 ]; then
            print_success "All tests passed!"
        else
            print_error "$failed_tests test(s) failed"
        fi
    else
        print_warning "Test results file not found"
    fi
    
    # Coverage summary
    local coverage_file="$COVERAGE_DIR/coverage.cobertura.xml"
    if [ -f "$coverage_file" ]; then
        local line_rate=$(grep -o 'line-rate="[0-9.]*"' "$coverage_file" | grep -o '[0-9.]*' | head -n 1)
        if [ -n "$line_rate" ]; then
            local coverage_percent=$(echo "scale=1; $line_rate * 100" | bc 2>/dev/null || echo "N/A")
            echo "📈 Code Coverage: ${coverage_percent}%"
        fi
    fi
    
    echo
    echo "📁 Generated Reports:"
    echo "   Test Results: $test_results_file"
    if [ -f "$COVERAGE_DIR/coverage.cobertura.xml" ]; then
        echo "   Coverage XML: $COVERAGE_DIR/coverage.cobertura.xml"
    fi
    if [ -f "$COVERAGE_DIR/html/index.html" ]; then
        echo "   Coverage HTML: $COVERAGE_DIR/html/index.html"
    fi
    
    echo
}

# Main execution
main() {
    local start_time=$(date +%s)
    
    print_header "JobService Build and Test Script"
    echo "Starting build process at $(date)"
    echo
    
    check_prerequisites
    create_coverlet_settings
    clean_solution
    restore_packages
    build_solution
    run_tests
    generate_coverage_html
    generate_test_summary
    
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    print_header "Build Complete"
    print_success "Total build time: ${duration} seconds"
    
    # Exit with error if tests failed
    local test_results_file="$REPORTS_DIR/test-results.trx"
    if [ -f "$test_results_file" ]; then
        local failed_tests=$(grep -o 'failed="[0-9]*"' "$test_results_file" | grep -o '[0-9]*' | head -n 1)
        failed_tests=${failed_tests:-0}
        
        if [ "$failed_tests" -gt 0 ]; then
            exit 1
        fi
    fi
}

# Help function
show_help() {
    echo "JobService Build Script"
    echo
    echo "Usage: $0 [options]"
    echo
    echo "Options:"
    echo "  -h, --help     Show this help message"
    echo "  -c, --clean    Clean only (skip build and test)"
    echo "  -b, --build    Build only (skip tests)"
    echo "  -t, --test     Test only (skip build)"
    echo
    echo "Examples:"
    echo "  $0              # Full build, test, and report generation"
    echo "  $0 --clean      # Clean solution only"
    echo "  $0 --build      # Build solution only"
    echo "  $0 --test       # Run tests only"
}

# Parse command line arguments
case "${1:-}" in
    -h|--help)
        show_help
        exit 0
        ;;
    -c|--clean)
        print_header "Clean Only Mode"
        check_prerequisites
        clean_solution
        print_success "Clean completed"
        exit 0
        ;;
    -b|--build)
        print_header "Build Only Mode"
        check_prerequisites
        clean_solution
        restore_packages
        build_solution
        print_success "Build completed"
        exit 0
        ;;
    -t|--test)
        print_header "Test Only Mode"
        check_prerequisites
        create_coverlet_settings
        run_tests
        generate_coverage_html
        generate_test_summary
        exit 0
        ;;
    "")
        # No arguments, run full build
        main
        ;;
    *)
        print_error "Unknown option: $1"
        show_help
        exit 1
        ;;
esac