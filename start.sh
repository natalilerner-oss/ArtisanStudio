#!/bin/bash

echo "🎨 Starting Artisan Studio..."
echo ""

# Check for required tools
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8: https://dotnet.microsoft.com/download"
    exit 1
fi

if ! command -v npm &> /dev/null; then
    echo "❌ Node.js not found. Please install Node.js 18+: https://nodejs.org/"
    exit 1
fi

# Install frontend dependencies if needed
if [ ! -d "frontend/node_modules" ]; then
    echo "📦 Installing frontend dependencies..."
    cd frontend && npm install && cd ..
fi

# Check for API key
if [ -z "$REPLICATE_API_KEY" ]; then
    echo "⚠️  REPLICATE_API_KEY not set. Running in demo mode."
    echo "   Get a free API key at: https://replicate.com/account/api-tokens"
    echo ""
fi

# Start backend in background
echo "🚀 Starting backend server..."
cd backend
dotnet run --urls "http://localhost:5000" &
BACKEND_PID=$!
cd ..

# Wait for backend to start
sleep 3

# Start frontend
echo "🎨 Starting frontend..."
cd frontend
npm run dev &
FRONTEND_PID=$!
cd ..

echo ""
echo "✅ Artisan Studio is running!"
echo ""
echo "   Frontend: http://localhost:3000"
echo "   Backend:  http://localhost:5000"
echo "   API Docs: http://localhost:5000/swagger"
echo ""
echo "Press Ctrl+C to stop..."

# Handle shutdown
trap "kill $BACKEND_PID $FRONTEND_PID 2>/dev/null" EXIT
wait
